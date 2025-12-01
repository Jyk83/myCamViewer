#include "stdafx.h"
#include "CAMViewData.h"
#include "gl\gl.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif // _DEBUG

//////////////////////////////////////////////////////////////////////////

bool camLayout::Init(const CAM_DATA& camFile)
{
	if (!FindArcsAndLines(camFile))
		return false;

	ASSERT(HasContents() && camFile.num_Parts == _num_parts);

	int iArc = 0;
	bool bScancut = false;
	for (int i = 0; i < _num_parts; ++i)
	{
		const CAM_SHAPE& p = camFile.pCAMShape[camFile.pPart[i].iCAMShape];
		if (0 >= p.numContours)
			continue;

		camPart& part = _part[i];

		double part_org_x = camFile.pPart[i].origin_X;
		double part_org_y = camFile.pPart[i].origin_Y;
		double part_org_r = camFile.pPart[i].origin_R;

		part.x_org = part_org_x;
		part.y_org = part_org_y;

		if (IsZero(part_org_r))
		{
			if (!Init(camFile.version, p, part, i, iArc, bScancut))
				return false;
		}
		else
		{
			if (!Init_TransPart(camFile.version, p, part, i, iArc, part_org_r, bScancut))
				return false;
		}

		if (bScancut)
			_isScancutIncluded = true;

		_numContoursTotal += part.num_contours;

	}// end of part

	return true;
}

bool camLayout::Init(fileVersion version, const CAM_SHAPE& p, camPart& part, int ipart, int& iArc, bool& bScancut)
{
	Rect2d& partBound = part.bound;
	partBound.x1 = partBound.x2 = p.pContour[0].x_start + part.x_org;
	partBound.y1 = partBound.y2 = p.pContour[0].y_start + part.y_org;

	for (int j = 0; j < p.numContours; ++j)
	{
		const CAM_CONTOUR& c = p.pContour[j];
		camContour& contour = part.pContour[j];
		Rect2d& contourBound = contour._bound;

		contour._iPart = ipart;
		contour._iContour = j;
		contour._lineNoStart = c.iCmdBlockStr + 1;
		contour._lineNoEnd = c.iCmdBlockStrLast + 1;

		if (c.bIsScancut)
			bScancut = true;

		if (verV08 == version)
		{
			contour._fPiercing = (PIERCING_NONE < c.nPiercing);
			contour._fMarking = (CUTTING_ENGRAVEMARKING == c.nCutting);
			contour._bHasLeadIn = !contour._fMarking;
			contour._bRenmantCut = c.bIsRemnant;
		}
		else if (verV16 == version || verV16A05 == version)
		{
			contour._fPiercing = (PIERCING_NONE < c.nPiercing);
			contour._fMarking = (CUTTING_V16_MARKING == c.nCutting);
			contour._bHasLeadIn = c.bHasLeadIn;
			contour._bRenmantCut = c.bIsRemnant;
			if (c.bIsRemnant)
				_vRenmantCutContours.push_back(&contour);
		}
		else
		{
			ASSERT (FALSE);
			return false;
		}

		contour._xo = c.x_start + part.x_org;
		contour._yo = c.y_start + part.y_org;

		contourBound.x1 = contourBound.x2 = contour._xo;
		contourBound.y1 = contourBound.y2 = contour._yo;

		double xo = contour._xo;
		double yo = contour._yo;

		for (int k = 0; k < c.numCodes; ++k)
		{
			camElement& elem = contour._pElement[k];
			elem._x1 = xo, elem._y1 = yo;

			const CAM_CODE& b = c.pCode[k];
			elem._x2 = b.X + part.x_org;
			elem._y2 = b.Y + part.y_org;

			ASSERT(NULL == elem._pArc);
			switch (b.code)
			{
			case G_Line:	// target position of the element has already been assigned above.
			case G_Jump:
				elem.CalcLineLength();
				contourBound.Union(elem._x2, elem._y2);
				break;

			case G_ArcCW:	// in case of arcs, we need to update arc information through camElement::UpdateArcInfo()
			case G_ArcCCW:	// bounding box information is also updated in the function
				elem.UpdateArcInfo(b, _pArcBuf[iArc++], contourBound);
				ASSERT(iArc <= _num_arcs);
				break;

			default:		// not possible to be here because CheckContours() has already checked the other case than line and arcs
				ASSERT(FALSE);
				return false;
				break;
			}

			elem._sBlock = b.sBlockCmd;
			elem._sBlock.Trim();
			elem._lineNo = b.nLineNo;

			xo = elem._x2, yo = elem._y2;

		}// end of element
		partBound.Union(contourBound);
		contour.CalcLength();
	}// end of contour
	return true;
}

bool camLayout::Init_TransPart(fileVersion version, const CAM_SHAPE& p, camPart& part, int ipart, int& iArc, const double& r_org, bool& bScancut)
{
	double radian = r_org / 180 * π;
	double sin_r = sin(radian);
	double cos_r = cos(radian);

	Rect2d& partBound = part.bound;
	partBound.x1 = partBound.x2 = (cos_r * p.pContour[0].x_start) - (sin_r * p.pContour[0].y_start) + part.x_org;
	partBound.y1 = partBound.y2 = (sin_r * p.pContour[0].x_start) + (cos_r * p.pContour[0].y_start) + part.y_org;

	for (int j = 0; j < p.numContours; ++j)
	{
		const CAM_CONTOUR& c = p.pContour[j];
		camContour& contour = part.pContour[j];
		Rect2d& contourBound = contour._bound;

		contour._iPart = ipart;
		contour._iContour = j;
		contour._lineNoStart = c.iCmdBlockStr + 1;
		contour._lineNoEnd = c.iCmdBlockStrLast + 1;

		if (c.bIsScancut)
			bScancut = true;

		if (verV08 == version)
		{
			contour._fPiercing = (PIERCING_NONE < c.nPiercing);
			contour._fMarking = (CUTTING_ENGRAVEMARKING == c.nCutting);
			contour._bHasLeadIn = !contour._fMarking;
			contour._bRenmantCut = c.bIsRemnant;
		}
		else if (verV16 == version || verV16A05 == version)
		{
			contour._fPiercing = (PIERCING_NONE < c.nPiercing);
			contour._fMarking = (CUTTING_V16_MARKING == c.nCutting);
			contour._bHasLeadIn = c.bHasLeadIn;
			contour._bRenmantCut = c.bIsRemnant;
			if (c.bIsRemnant)
				_vRenmantCutContours.push_back(&contour);
		}
		else
		{
			ASSERT(FALSE);
			return false;
		}


		contour._xo = (cos_r * c.x_start) - (sin_r * c.y_start) + part.x_org;
		contour._yo = (sin_r * c.x_start) + (cos_r * c.y_start) + part.y_org;

		contourBound.x1 = contourBound.x2 = contour._xo;
		contourBound.y1 = contourBound.y2 = contour._yo;

		double xo = contour._xo;
		double yo = contour._yo;

		//회전하기 전의 시작 좌표값
		double x_org_start = c.x_start;
		double y_org_start = c.y_start;

		for (int k = 0; k < c.numCodes; ++k)
		{
			camElement& elem = contour._pElement[k];
			elem._x1 = xo, elem._y1 = yo;

			const CAM_CODE& b = c.pCode[k];

			ASSERT(NULL == elem._pArc);

			switch (b.code)
			{
			case G_Line:	// target position of the element has already been assigned above.
			case G_Jump:
				elem._x2 = (cos_r * b.X) - (sin_r * b.Y) + part.x_org;
				elem._y2 = (sin_r * b.X) + (cos_r * b.Y) + part.y_org;
				elem.CalcLineLength();
				contourBound.Union(elem._x2, elem._y2);
				break;

			case G_ArcCW:	// in case of arcs, we need to update arc information through camElement::UpdateArcInfo()
			case G_ArcCCW:	// bounding box information is also updated in the function
				elem.UpdateArcInfo(b, _pArcBuf[iArc++], contourBound, cos_r, sin_r, part.x_org, part.y_org, x_org_start, y_org_start);
				ASSERT(iArc <= _num_arcs);
				break;

			default:		// not possible to be here because CheckContours() has already checked the other case than line and arcs
				ASSERT(FALSE);
				return false;
				break;
			}

			elem._sBlock = b.sBlockCmd;
			elem._sBlock.Trim();
			elem._lineNo = b.nLineNo;

			xo = elem._x2, yo = elem._y2;

			x_org_start = b.X, y_org_start = b.Y;

		}// end of element
		partBound.Union(contourBound);
		contour.CalcLength();
	}// end of contour
	return true;
}


bool camLayout::FindArcsAndLines(const CAM_DATA& camFile)
{
	if (!camFile.HasContents())
	{
		ASSERT(FALSE);
		return false;
	}

	Delete();

	//
	// First, count the number of arcs and lines
	//
	int count_parts = camFile.num_Parts;
	ASSERT(0 < count_parts);	// because CAMFILEINFO::HasContents() returned true above
	_part = new camPart[count_parts];
	if (!_part)
		return false;

	for (int i = 0; i < count_parts; ++i)
	{
		const CAM_SHAPE& p = camFile.pCAMShape[camFile.pPart[i].iCAMShape];
		if (0 >= p.numContours)
			continue;

		camPart& part = _part[i];
		part.fileLineNo = camFile.pPart[i].iCmdBlockStr + 1;

		part.pContour = new camContour[p.numContours];
		if (!part.pContour)
			goto MemAllocError;
		part.num_contours = p.numContours;

		for (int j = 0; j < p.numContours; ++j)
		{
			const CAM_CONTOUR& c = p.pContour[j];
			camContour& contour = part.pContour[j];
			contour._lineNoStart = c.iCmdBlockStr + 1;
			contour._lineNoEnd = c.iCmdBlockStrLast + 1;

			for (int k = 0; k < c.numCodes; ++k)
			{
				switch (c.pCode[k].code)
				{
				case G_Line:	break;
				case G_ArcCW:	case G_ArcCCW:	++_num_arcs;	break;
				case G_Jump:	break;
				default: ASSERT(FALSE); return false;			break;
				}
			}

			_part[i].pContour[j]._pElement = new camElement[c.numCodes];
			if (!_part[i].pContour[j]._pElement)
				goto MemAllocError;
			_part[i].pContour[j]._numElements = c.numCodes;
		}
	}

	//
	// Allocate the arc buffers
	//
	if (0 < _num_arcs)
	{
		_pArcBuf = new arcInfo[_num_arcs];
		if (!_pArcBuf)
		{
			delete[] _part;
			_part = NULL;
			_num_arcs = 0;
			return false;
		}
	}

	//
	// Complete the layout information
	//
	_num_parts = count_parts;
	_width = camFile.sheetWidth;
	_height = camFile.sheetHeight;

	return true;

MemAllocError:
	delete[] _part;
	_part = NULL;
	return false;
}


void camLayout::Delete()
{
	if (_part)
		delete[] _part;
	if (_pArcBuf)
		delete[] _pArcBuf;

	_vRenmantCutContours.clear();

	_width = _height = 0;
	_isScancutIncluded = false;
	_num_parts = 0;
	_part = nullptr;
	_num_arcs = 0;
	_pArcBuf = nullptr;

	return;
}

int camContour::GetElement(int iStart, CStringA& block) const
{
	for (int i = iStart; i < _numElements; ++i)
	{
		if (_pElement[i]._sBlock.Find(block) != -1)
			return i;
	}
	return -1;
}

int camContour::GetElement(int iStart, double x, double y, double& progress) const
{
	int iElemOnSpot = -1;
	double minDeviation = DBL_MAX, d, p;
	const Point2d point(x, y);
	for (int i = iStart; i < _numElements; ++i)
	{
		if (_pElement[i].PtOnPath(point, d, p))
		{
			ASSERT(GE(d, 0) && GE(p, 0) && LE(p, 1));
			if (CAMNumeric::Equal(d, 0.0))
			{
				progress = minof(maxof(0.0, p), 1.0);
				return i;
			}
			else if (d < minDeviation)
			{
				minDeviation = d;
				iElemOnSpot = i;
				progress = minof(maxof(0.0, p), 1.0);
			}
		}
	}
	return iElemOnSpot;
}

bool camContour::GetProgress(int iElem, const Point2d& point, double& progress) const
{
	if (0 > iElem || _numElements <= iElem)
	{
		ASSERT(FALSE);
		return false;
	}
	double deviation;
	if (!_pElement[iElem].PtOnPath(point, deviation, progress))
		return false;
	if (1 < deviation)
		return false;
	return true;
}

void camContour::CalcLength()
{
	if (_numElements <= 0)
		return;

	if(_pElement == NULL)
		return;

	for (int i = 0; i < _numElements; ++i)
		_length += _pElement[i]._length;
}

double camContour::GetLength(int untilElement) const
{
	if (_numElements <= 0)
		return 0;

	if (_pElement == NULL)
		return 0;

	if (_numElements <= untilElement)
		return GetLength();

	double length = 0;
	for (int i = 0; i <= untilElement; ++i)
		length += _pElement[i]._length;

	return length;
}

void camElement::UpdateArcInfo(const CAM_CODE& b, arcInfo& arc, Rect2d& contourBox)
{
	ASSERT(NULL == _pArc && (G_ArcCW==b.code || G_ArcCCW==b.code));

	// general information about the arc
	arc.xc = _x1 + b.I;
	arc.yc = _y1 + b.J;
	arc.r = sqrt(b.I*b.I + b.J*b.J);

	// sweeping angle information
	double x1 = -b.I, y1 = -b.J;					// start position on the arc translated to its center position
	double x2 = _x2 - arc.xc, y2 = _y2 - arc.yc;	// end position on the translated arc
	arc.θ1 = NormalizeRad(atan2(y1, x1));
	arc.θ2 = NormalizeRad(atan2(y2, x2));

	if (G_ArcCW==b.code)
	{
		arc.isClockwise = true;
		arc.dθ = IsSame(arc.θ1, arc.θ2) ? π2 : NormalizeRad(arc.θ1 - arc.θ2);
	}
	else
	{
		arc.isClockwise = false;
		arc.dθ = IsSame(arc.θ1, arc.θ2) ? π2 : NormalizeRad(arc.θ2 - arc.θ1);
	}
	_length = arc.r * arc.dθ;

	// bounding box information
	Rect2d box;
	if (arc.isClockwise)
		arc.GetCWBoundingBox(x1, y1, x2, y2, box);
	else
		arc.GetCCWBoundingBox(x1, y1, x2, y2, box);
	contourBox.Union(box);

	_pArc = &arc;

	return;
}

void camElement::UpdateArcInfo(const CAM_CODE& b, arcInfo& arc, Rect2d& contourBox, const double& cos_r, const double& sin_r, const double& part_orgx, const double& part_orgy
	, const double& x_org, const double& y_org)
{
	ASSERT(NULL == _pArc && (G_ArcCW == b.code || G_ArcCCW == b.code));

	// general information about the arc
	arc.xc = (cos_r * (x_org + b.I)) - (sin_r * (y_org + b.J)) + part_orgx;
	arc.yc = (sin_r * (x_org + b.I)) + (cos_r * (y_org + b.J)) + part_orgy;

	double I = cos_r * b.I - sin_r * b.J;
	double J = sin_r * b.I + cos_r * b.J;

	arc.r = sqrt(I*I + J*J);

	_x2 = (cos_r * b.X) - (sin_r * b.Y) + part_orgx;
	_y2 = (sin_r * b.X) + (cos_r * b.Y) + part_orgy;

	// sweeping angle information
	double x1 = -I, y1 = -J;					// start position on the arc translated to its center position
	double x2 = _x2 - arc.xc, y2 = _y2 - arc.yc;	// end position on the translated arc

	arc.θ1 = NormalizeRad(atan2(y1, x1));
	arc.θ2 = NormalizeRad(atan2(y2, x2));

	if (G_ArcCW == b.code)
	{
		arc.isClockwise = true;
		arc.dθ = IsSame(arc.θ1, arc.θ2) ? π2 : NormalizeRad(arc.θ1 - arc.θ2);
	}
	else
	{
		arc.isClockwise = false;
		arc.dθ = IsSame(arc.θ1, arc.θ2) ? π2 : NormalizeRad(arc.θ2 - arc.θ1);
	}
	_length = arc.r * arc.dθ;

	// bounding box information
	Rect2d box;
	if (arc.isClockwise)
		arc.GetCWBoundingBox(x1, y1, x2, y2, box);
	else
		arc.GetCCWBoundingBox(x1, y1, x2, y2, box);
	contourBox.Union(box);

	_pArc = &arc;

	return;
}


bool camElement::PtOnPath(const Point2d& pt, double& deviation, double& progress) const
{
	if (0 == _length)
	{
		ASSERT(FALSE);
		return false;
	}

	if (IsArc())
	{
		const arcInfo& arc = *_pArc;
		const double xo = arc.xc, yo = arc.yc;
		Point2d p1(_x1 - xo, _y1 - yo), p2(_x2 - xo, _y2 - yo), p(pt.x - xo, pt.y - yo);
		// First, check if the point can be placed on the circle
		const double OP = p.Magnitude();
		if (IsZero(OP) || 0.1 < Abs(OP-arc.radius()))
			return false;
		// And then, check if the point location falls on the arc
		if (arc.clockwise())
		{
			p1 = Point2d(_x2 - xo, _y2 - yo);
			p2 = Point2d(_x1 - xo, _y1 - yo);
		}
		const double P1 = p1.Magnitude();
		double angle = 0;
		if (LE(arc.angle(), π))	// acute angle
		{
			if (IsNegative(p1.Cross(p)) || IsNegative(p.Cross(p2)))
				return false;
			double cosine = p1.Dot(p)/(P1*OP);
			cosine = minof(maxof(-1.0, cosine), 1.0);
			angle = ZeroLimit(acos(p1.Dot(p)/(P1*OP)));
		}
		else	// obtuse angle
		{
			if (IsPositive(p.Cross(p1)) && IsPositive(p2.Cross(p)))
				return false;
			double cosine = p1.Dot(p)/(P1*OP);
			cosine = minof(maxof(-1.0, cosine), 1.0);
			angle = IsSame(cosine, 1)? 0: IsSame(cosine, -1)? π: ZeroLimit(acos(cosine));
			ASSERT(GE(angle, 0) && LE(angle, π));
			if (IsNegative(p1.Cross(p)))
				angle = π2 - angle;
		}
		deviation = ZeroLimit(arc.radius()-OP);
		progress = arc.clockwise()? (1.0-angle/arc.angle()): (angle/arc.angle());
		LimitToZero(progress);
		if (1.0 < progress)
			progress = 1;
		return true;
	}
	else
	{
		Point2d va(_x2-_x1, _y2-_y1);
		Point2d vb(pt.x-_x1, pt.y-_y1);
		ASSERT(CAMNumeric::Equal(_length, va.Magnitude()));

		double directionCosine = ZeroLimit(va.Dot(vb)/_length);
		if (IsNegative(directionCosine) || GT(directionCosine, _length))
			return false;

		deviation = ZeroLimit(Abs(va.Cross(vb)/_length));
		progress = directionCosine/_length;
	}
	return true;
}

bool camElement::PtOnPath(double wcsX, double wcsY, double& cutDone, double& cutRemains) const
{
	Point2d pt(wcsX, wcsY);
	double deviation = 0, progress = 0;
	if (!PtOnPath(pt, deviation, progress))
		return false;

	if (0.2 < deviation)
		return false;

	cutDone = _length * progress;
	cutRemains = _length - cutDone;
	return true;
}


// bool arcInfo::InsideArc(const Point2d& p, const Point2d& r1, const Point2d& r2, double& deviation, double& fraction) const
// {
// 	Point2d b(p.x-xc, p.y-yc);
// 	double B = b.Magnitude();
// 	if (IsZero(B) || 0.1 < Abs(B-r))
// 		return false;
// 
// 	Point2d a, c;
// 	if (isClockwise)
// 	{
// 		a = Point2d(r2.x - xc, r2.y - yc);
// 		c = Point2d(r1.x - xc, r1.y - yc);
// 	}
// 	else
// 	{
// 		a = Point2d(r1.x - xc, r1.y - yc);
// 		c = Point2d(r2.x - xc, r2.y - yc);
// 	}
// 
// 	double A = a.Magnitude();
// 	double cosine = a.Dot(b)/(A*B);
// 	ASSERT(GE(cosine, -1.0) && LE(cosine, 1.0));
// 
// 	double angle = 0;
// 	if (LE(dθ, π))	// acute angle
// 	{
// 		if (IsNegative(b.Cross(c)) || IsNegative(a.Cross(b)))
// 			return false;
// 		angle = IsSame(cosine, 1)? 0: IsSame(cosine, -1)? π: ZeroLimit(acos(cosine));
// 	}
// 	else	// obtuse angle
// 	{
// 		if (IsPositive(b.Cross(a)) && IsPositive(c.Cross(b)))
// 			return false;
// 		angle = IsSame(cosine, 1)? 0: IsSame(cosine, -1)? π: ZeroLimit(acos(cosine));
// 		ASSERT(GE(angle, 0) && LE(angle, π));
// 		if (IsNegative(a.Cross(b)))
// 			angle = π2 - angle;
// 	}
// 	deviation = Abs(r-B);
// 	fraction = isClockwise? minof(ZeroLimit(1.0-angle/dθ), 1.0): minof(ZeroLimit(angle/dθ), 1.0);
// 
// 	return true;
// }


void arcInfo::GetCWBoundingBox(double x1, double y1, double x2, double y2, Rect2d& box) const
{
	ASSERT (true == isClockwise);
	double start_x = x1 + xc, end_x = x2 + xc;
	double start_y = y1 + yc, end_y = y2 + yc;

	if (0 <= x1)
	{
		if (0 <= x2)	// 0<=x1 && 0<=x2: start & end positions fall in the right semicircle
		{
			if (y1 <= y2)		// sweeping through the other end of the circle
			{
				box.x1 = xc - r;
				box.x2 = (y1<0 && 0<y2)? max(start_x, end_x): xc + r;
				box.y1 = yc - r;
				box.y2 = yc + r;
			}
			else /* y2 < y1 */	// narrow slice of falls in the right semicircle
			{
				if (y1<0 && 0<y2)	// => x1 @ 1st quadrant && x2 @ 4th quadrant
				{
					box.x1 = min(start_x, end_x);
					box.x2 = xc + r;
				}
				else
				{
					if (0 <= y2)
					{
						box.x1 = start_x;
						box.x2 = end_x;
					}
					else
					{
						box.x1 = end_x;
						box.x2 = start_x;
					}
				}
				box.y1 = end_y;
				box.y2 = start_y;
			}
		}
		else			// x2<0 && 0<=x1: start & end positions are apart across the y-axis
		{
			if (0 <= y1)
			{
				box.x1 = (0 <= y2)? xc - r: end_x;
				box.x2 = xc + r;
			}
			else // y1 < 0
			{
				box.x1 = (0 <= y2)? xc - r: end_x;
				box.x2 = start_x;
			}
			box.y1 = yc - r;
			box.y2 = max(start_y, end_y);
		}
	}
	else
	{
		if (0 <= x2)	// x1<0 && 0<=x2: start & end positions are apart across the y-axis
		{
			box.x1 = (0 <= y1)? start_x: xc - r;
			box.x2 = (0 <= y2)? end_x: xc + r;
			box.y1 = min(start_y, end_y);
			box.y2 = yc + r;
		}
		else			// x1<0 && x2<0: start & end positions fall in the right semicircle
		{
			if (y1 <= y2)
			{
				if (y1<=0 && 0<=y2)
				{
					box.x1 = xc - r;
					box.x2 = max(start_x, end_x);
				}
				else
				{
					if (start_x <= end_x)
					{	ASSERT(0 <= y1);
						box.x1 = start_x;
						box.x2 = end_x;
					}
					else
					{	ASSERT(y2 <= 0);
						box.x1 = end_x;
						box.x2 = start_x;
					}
				}
				box.y1 = start_y;
				box.y2 = end_y;
			}
			else // y2 < y1
			{
				box.x1 = (y1<=0 && 0<=y2)? box.x1 = min(start_x, end_x): xc - r;
				box.x2 = xc + r;
				box.y1 = end_y;
				box.y2 = yc + r;
			}
		}
	}
	return;
}


void arcInfo::GetCCWBoundingBox(double x1, double y1, double x2, double y2, Rect2d& box) const
{
	ASSERT (false == isClockwise);
	if (IsSame(x1, x2) && IsSame(y1, y2))
	{
		box.x1 = xc-r, box.x2 = xc+r;
		box.y1 = yc-r, box.y2 = yc+r;
		return;
	}

	double start_x = x1 + xc, end_x = x2 + xc;
	double start_y = y1 + yc, end_y = y2 + yc;

	if (0 <= x1)
	{
		if (0 <= x2)	// 0<=x1 && 0<=x2: start & end positions fall in the right semicircle
		{
			if (y1 <= y2)		// narrow slice of falls in the right semicircle
			{
				if (y1<0 && 0<y2)	// => (y2@1st quadrant and y1 @4th quadrant)
				{
					box.x1 = min(start_x, end_x);
					box.x2 = xc + r;
				}
				else // both of start and end @ 1st quadrant or @ 4th quadrant
				{
					if (0 <= y1)
					{
						box.x1 = end_x;
						box.x2 = start_x;
					}
					else
					{
						box.x1 = start_x;
						box.x2 = end_x;
					}
				}
				box.y1 = start_y;
				box.y2 = end_y;
			}
			else /* y2 > y1*/	// sweeping through the other end of the circle
			{
				box.x1 = xc - r;
				box.x2 = (0<y1 && y2<0)? max(start_x, end_x): xc + r;
				box.y1 = yc - r;
				box.y2 = yc + r;
			}
		}
		else	// x2<0 && 0<=x1: start & end positions are apart across the y-axis
		{
			box.x1 = (0 <= y2)? end_x: xc - r;
			box.x2 = (0 <= y1)? start_x: xc + r;
			box.y1 = min(start_y, end_y);
			box.y2 = yc + r;
		}
	}
	else // x1 < 0
	{
		if (0 <= x2)	// x1<0 && 0<=x2: start & end positions are apart across the y-axis
		{
			box.x1 = (0 <= y1)? xc - r: start_x;
			box.x2 = (0 <= y2)? xc + r: end_x;
			box.y1 = yc - r;
			box.y2 = max(start_y, end_y);
		}
		else			// x1<0 && x2<0: start & end positions fall in the right semicircle
		{
			if (y1 <= y2)
			{
				box.x1 = (y1<0 && 0<y2)? min(start_x, end_x): xc - r;
				box.x2 = xc + r;
				box.y1 = yc - r;
				box.y2 = yc + r;
			}
			else // y2 < y1
			{
				if (0<y1 && y2<0)
				{
					box.x1 = xc - r;
					box.x2 = max(start_x, end_x);
				}
				else
				{
					if (start_x <= end_x)
					{	ASSERT(y1 <= 0);
						box.x1 = start_x;
						box.x2 = end_x;
					}
					else
					{	ASSERT(0 <= y2);
						box.x1 = end_x;
						box.x2 = start_x;
					}
				}
				box.y1 = end_y;
				box.y2 = start_y;
			}
		}
	}
	return;
}

void arcInfo::glDraw(double length, double scale) const
{
	ASSERT(IsSame(length, r*dθ));

	int num_arc_segments = int(length*scale/1.4);
	int min_arc_segments = int(dθ/unit_rad);
	num_arc_segments = max(max(3, min_arc_segments), num_arc_segments);
	ASSERT(3 <= num_arc_segments);

	double factor = isClockwise? -dθ/num_arc_segments: dθ/num_arc_segments;
	for (int i = 1; i < num_arc_segments-1; ++i)
	{
		double angle = θ1 + i*factor;
		glVertex2d(xc + r*cos(angle), yc+r*sin(angle));
	}
}

void arcInfo::glDraw(double length, double scale, double progress) const
{
	ASSERT(IsSame(length, r*dθ));

	length *= progress;
	double angle = progress*dθ;

	int num_arc_segments = int(length*scale/1.4);
	int min_arc_segments = int(angle/unit_rad);
	num_arc_segments = max(max(3, min_arc_segments), num_arc_segments);
	ASSERT(3 <= num_arc_segments);

	double factor = isClockwise? -angle/num_arc_segments: angle/num_arc_segments;
	for (int i = 1; i < num_arc_segments-1; ++i)
	{
		double θ = θ1 + i*factor;
		glVertex2d(xc + r*cos(θ), yc+r*sin(θ));
	}

	angle = isClockwise? θ1-angle: θ1+angle;
	glVertex2d(xc + r*cos(angle), yc + r*sin(angle));
}
