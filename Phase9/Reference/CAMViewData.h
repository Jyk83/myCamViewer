#pragma once

#include "Graph2d.h"
#include "MPFInterface.h"
#include <vector>


struct arcInfo
{
	double	r;		// radius of curvature
	double	xc, yc;	// center of curvature
	double	еш1, еш2;	// the start and the end angle
	double	dеш;		// sweeping angle

	bool isClockwise;// sweeping direction

	bool	clockwise() const { return isClockwise; }
	double  radius() const { return r; }
	double  angle() const { return dеш; }
	double	AngleAt(double fraction) const { return isClockwise? еш1-fraction*dеш: еш1+fraction*dеш; }
//	bool	InsideArc(const Point2d& p, const Point2d& r1, const Point2d& r2, double& deviation, double& fraction) const;
	void	GetCWBoundingBox(double x1, double y1, double x2, double y2, Rect2d& box) const;
	void	GetCCWBoundingBox(double x1, double y1, double x2, double y2, Rect2d& box) const;
	void	glDraw(double length, double scale) const;
	void	glDraw(double length, double scale, double progress) const;
};


struct camElement
{
	int			_lineNo;	// line number in the source file
	double		_x1, _y1;	// start position of the element
	double		_x2, _y2;	// end position of the element
	double		_length;	// length of the element; redundant information for the drawing performance
	arcInfo*	_pArc;		// null => straight line, otherwise an arc

	CStringA	_sBlock;	// the original block statement in the MPF file
	bool		_cutDone;	// whether it has been cut or not
	double		_progress;	// cutting progress if it's being cut

	camElement() : _pArc(0), _length(0), _cutDone(false), _progress(0) {}
	void CalcLineLength() { _length = sqrt((_x2-_x1)*(_x2-_x1)+(_y2-_y1)*(_y2-_y1)); }
	void UpdateArcInfo(const CAM_CODE& b, arcInfo& arc, Rect2d& contourBox);
	void UpdateArcInfo(const CAM_CODE& b, arcInfo& arc, Rect2d& contourBox, const double& cos_r, const double& sin_r, const double& part_orgx, const double& part_orgy
		, const double& x_org, const double& y_org);

	bool IsLine() const { return (NULL == _pArc); }
	bool IsArc() const { return (NULL != _pArc); }
	bool IsClockwise() const { return (IsArc() && _pArc->isClockwise); }
	double GetLength() const { return _length; }
	void Coords(double fraction, double& x, double& y) const
	{
		ASSERT(0 <= fraction && LE(fraction, 1));

		if (IsLine())
		{
			double q = 1-fraction;
			x = q*x + fraction*_x2;
			y = q*y + fraction*_y2;
		}
		else
		{
			double еш = _pArc->AngleAt(fraction);
			x = _pArc->r*cos(еш) + _pArc->xc;
			y = _pArc->r*sin(еш) + _pArc->yc;
		}
	}

	bool PtOnPath(const Point2d& pt, double& deviation, double& progress) const;
	bool PtOnPath(double wcsX, double wcsY, double& cutDone, double& cutRemains) const;
};


struct camContour
{
	Rect2d	_bound;		// bounding rectangle of the contour
	double	_length;	// total length of the contour
	int		_lineNoStart;// start line number in the source file
	int		_lineNoEnd;	// end line number in the source file
	double	_xo, _yo;	// Start position of contour in WCS
						//	Nesting CAM_PART.origin_X + CAM_SHAPE, CAM_CONTOUR.x_start
						//	Nesting CAM_PART.origin_Y + CAM_SHAPE, CAM_CONTOUR.y_start
	// CAM information
	int		_numElements;
	camElement* _pElement;

	// index of part and contour to which this contour is assigned
	int		_iPart;		//Array index of the entire parts
	int		_iContour;	//Array index of the entire contours

	// CAM options
	bool	_fPiercing;
	bool	_fMarking;
	bool	_bHasLeadIn;
	bool	_bCuttingDone;
	bool	_bRenmantCut;

	camContour() : _fPiercing(false), _fMarking(false), _bHasLeadIn(true), _bCuttingDone(false), _bRenmantCut(false), _numElements(0), _pElement(0), _length(0), _iPart(0), _iContour(0) {}
	~camContour() { delete[] _pElement; }

	int GetElement(int iStart, CStringA& block) const;
	int GetElement(int iStart, double x, double y, double& progress) const;
	void CalcLength();
	double GetLength() const { return _length; }
	double GetLength(int untilElement) const;
	bool GetProgress(int iElem, const Point2d& pos, double& progress) const;
};


struct camPart
{
	int		fileLineNo;		// nesting line number in the source file
	double	x_org, y_org;	// Workspace origin coordinates

	int		num_contours;	// All the coordinates of each contour and its elements are stored
	camContour* pContour;	// in the world coordinates including the workpiece origin coordinates' offsets.

	Rect2d	bound;			// The bounding box coordinates are also represented in the world coordinates.

	camPart() : num_contours(0), pContour(0) {}
	~camPart() { delete[] pContour; }
};


struct camLayout
{
	double		_width, _height;
	bool		_isScancutIncluded;
	int			_num_parts;
	camPart*	_part;
	int			_num_arcs;
	arcInfo*	_pArcBuf;

	int	_numContoursTotal;	// sum total of contours out of _part[]

	std::vector<camContour*> _vRenmantCutContours;	//All of the memory addresses of the remnant cut contours.

	camLayout() : _width(0.0), _height(0.0), _isScancutIncluded(false),
		_num_parts(0), _part(nullptr), _num_arcs(0), _pArcBuf(nullptr), _numContoursTotal(0),
		_lineNo_NestingStart(0), _lineNo_NestingEnd(0), _lineNo_CAMStart(0), _lineNo_CAMEnd(0)
	{
	}
	~camLayout() { delete[] _part; delete[] _pArcBuf; _vRenmantCutContours.clear(); }

	bool Init(const CAM_DATA& camData);
	bool Init(fileVersion version, const CAM_SHAPE& p, camPart& part, int ipart, int& iArc, bool& bScancut);
	bool Init_TransPart(fileVersion version, const CAM_SHAPE& p, camPart& part, int ipart, int& iArc, const double& r_org, bool& bScancut);
	bool HasContents() const { return (0 < _num_parts && 0 < _width && 0 < _height && NULL != _part); }
	void Delete();

protected:
	int _lineNo_NestingStart;
	int _lineNo_NestingEnd;
	int _lineNo_CAMStart;
	int _lineNo_CAMEnd;
	bool FindArcsAndLines(const CAM_DATA& data);	// check contour types, numbers, and allocate buffers accordingly
};

namespace CAMNumeric
{
const double _epsilon = 1.0e-7;
const double _tolerance = 1.0e-4;
inline bool Equal(double a, double b) { return (a > b)? (a-b <= _tolerance): (b-a <= _tolerance); }
}
