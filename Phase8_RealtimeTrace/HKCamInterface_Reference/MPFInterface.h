#pragma once


#include "float.h"
#include "Graph2d.h"

//////////////////////////////////////////////////////////////////////////
// MPF file structure (V08)
// 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
// 弛N1                                            弛 - Part nesting information, such as
// 弛HKLDB(1, "MSHRO023")                          弛   1. Cutting DB loader call(DB folder[1=O2, 2=N2], "DB Name")
// 弛HKINI(335.1, 335.1, 0, 0, 0)                  弛   2. Initialization call(sheet width, sheet height, 3 unused)
// 弛N10000 HKOST(10., 10., 0.00, 10001)           弛   3. Part #1 origin of CAM object #1 (starting from N1001)
// 弛N20000 HKOST(120., 120., 0.00, 10001)         弛   4. Part #2 origin of CAM object #1 ( " )
// 弛N30000 HKOST(230., 230., 0.00, 10001)         弛   5. Part #3 origin of CAM object #1 ( " )
// 弛N40000 HKEND                                  弛   6. Finalization sub-program call
// 弛N10 M30                                       弛   7. End of the part program
// 弛                                              弛 - Part CAM information
// 弛N10001 HKLON(2, 2, 32.562, 23.042, 15., 15.)  弛   1. Go to the start position of the CAM object with laser-ON
// 弛HKTON(0)                                      弛   2. Preparation of tool-offset and gap sensing
// 弛G1 X36.026 Y19.577                            弛   3. G-codes for geometrical movement of the cutting head
// 弛G2 X25.632 Y30.113 I-5.161 J5.303             弛      :
// 弛HKTOF(2, 36.097, 19.648, 5.233, -5.233)       弛   4. G-code for the last element
// 弛HKLOF(1)                                      弛   5. Laser-OFF
// 弛N10002 HKLON(2, 2, 52.839, 22.512, 15., 15.)  弛
// 弛HKTON(0)                                      弛 For each CAM object,
// 弛G1 X56.303 Y19.047                            弛  the HKLON-HKLOF snippet defines the geometry of a contour
// 弛G2 X45.909 Y29.583 I-5.161 J5.303             弛  and inside that HKLON-HKLOF snippet,
// 弛HKTOF(2, 56.374, 19.118, 5.233, -5.233)       弛  the G-code's with HKTOF arguments show how the elements make up the contour.
// 弛HKLOF(1)                                      弛
// 弛 :                                            弛  This HKLON-HKLOF snippets finish single, whole CAM object with
// 弛 :                                            弛
// 弛HKTOF(3, 85.426, 85.426, 35.426, 35.426)      弛
// 弛N10011 HKPED                                  弛  HKPED is inserted just before the last HKLOF.
// 弛HKLOF(1)                                      弛
// 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
//
// MPF file structure (V16)
// 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
// 弛N1                                            弛 - Part nesting information, such as
// 弛HKLDB(1, "DBName", 2, 0, 0, 0)                弛   1. Cutting DB loader call(Material type, DB name, Assistant gas type, 3 reserved parameters)
// 弛HKINI(100, 200, 200, 0, 0, 0)                 弛   2. Initialization call(number of parts, sheet width, sheet height, 3 reserved parameters)
// 弛N10000 HKOST(1.,1.,0.,10001,2, 0, 0, 0)       弛   3. Part #1  origin setting(x-pos, y-pos, rotation angle, starting statement number, number of contours, 3 reserved parameters)
// 弛HKPPP                                         弛   4. Calculation of the next statement number
// 弛N10001 HKOST(...)                             弛   5. Rest of parts origin settings
// 弛HKPPP                                         弛   6. Calculation of the next statement number
// 弛 :                                            弛       :
// 弛N20000 HKEND                                  弛   7. Finalization sub-program call
// 弛N10 M30                                       弛   8. End of the part program
// 弛                                              弛 - Part CAM information (piercing/lead-in/cutting)
// 弛N10001 HKSTR(1, 1, 50., 110., 1, 100.,110.,0) 弛   1. Part start(piercing type, cutting type, start x, start y, kerf width correction, part width, part height, reserved)
// 弛HKPIE(0,0,0)                                  弛   2. Piercing (3 reserved parameters)
// 弛HKLEA(0,0,0)                                  弛   3. Lead-In (3 reserved parameters)
// 弛G1 X100 Y100                                  弛
// 弛HKCUT(0,0,0)                                  弛
// 弛G-code list for part drawing                  弛
// 弛WHEN ($AC_TIME>0.05)AND($R71<$R72)AND...      弛
// 弛G1 X100 Y100                                  弛
// 弛HKSTO(0,0,0)                                  弛
// 弛 :                                            弛
// 弛N10002 HKSTR(...)                             弛
// 弛HKPIE(...)                                    弛
// 弛HKLEA(...)                                    弛
// 弛G1 ...                                        弛
// 弛HKCUT(...)                                    弛
// 弛G-codes...                                    弛
// 弛WHEN(...) DO $A_DBB[10]=1                     弛
// 弛G1...                                         弛
// 弛HKSTO(...)                                    弛
// 弛 :                                            弛
// 弛                                              弛
// 弛N10XXX HKPED(0,0,0)                           弛
// 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
//


enum fileVersion { verUnknown, verV08, verV16, verV16A05 };

//
//////////////////////////////////////////////////////////////////////////

enum G_Code
{
	G_Jump,		// G0
	G_Line,		// G1
	G_ArcCW,	// G2
	G_ArcCCW,	// G3
	G_Unknown,
};

LPCTSTR getPiercingTypeName(int iPiercing);
LPCTSTR getCuttingTypeName(int iCutting);
LPCTSTR getFileVersionName(fileVersion version);

//////////////////////////////////////////////////////////////////////////

#define PIERCING_NONE				0
#define PIERCING_PULSE				1
#define PIERCING_CW					2
#define PIERCING_DOWN				3
#define PIERCING_MP					4
#define PIERCING_QUICK				5
#define PIERCING_SHOTMARKING		8
#define PIERCING_RAPID				9

#define CUTTING_UNDEFINED			-1
#define CUTTING_SHOTMARKING			0
#define CUTTING_HIGHSPEED			1
#define CUTTING_MIDDLESPEED			2
#define CUTTING_LOWSPEED			3
#define CUTTING_SPECIAL				4
#define CUTTING_ENGRAVEMARKING		11

#define PIERCING_V16_NONE			0
#define PIERCING_V16_NORMAL			1
#define PIERCING_V16_SHOTMARKING	10
#define PIERCING_V16_REPEAT			11

#define CUTTING_V16_NORMAL			1	// large contour
#define CUTTING_V16_PULSE			2	// small contour
#define CUTTING_V16_MARKING			10
#define CUTTING_V16_REPEAT			11

//////////////////////////////////////////////////////////////////////////

class CAM_DATA;
struct CAM_CODE;
struct CAM_CONTOUR;
struct CAM_SHAPE;
struct CAM_PART;

enum LogLevel
{
	enLogError = 0x01,		// 0001
	enLogWarning = 0x03,	// 0011
	enLogInfo = 0x07,		// 0111
};

//////////////////////////////////////////////////////////////////////////

class FileWriter
{
public:
	FileWriter() : _fp(0) { _szError[0] = 0; }
	~FileWriter() { if (_fp) fclose(_fp); }

	bool Open(LPCTSTR pszFile, bool isAppendingToExsting=false)
	{
		if (_fp)
		{
			fclose(_fp);
			_fp = NULL;
		}
		LPCTSTR szMode = isAppendingToExsting? _T("a+"): _T("w");
		_result = _tfopen_s(&_fp, pszFile, szMode);
		if (_result)
		{
			_tcserror_s(_szError, _result);
			return false;
		}
		return true;
	}

	bool WriteString(LPCTSTR pszLine)
	{
		if (_fp)
		{
			return (EOF != _fputts(pszLine, _fp));
		}
		else
		{
			ASSERT(FALSE);
			return false;
		}
	}

	bool WriteStrings(CStringArray& fileText)
	{
		if (_fp)
		{
			for (int i = 0; i < fileText.GetCount(); ++i)
				_fputts(fileText.GetAt(i)+_T('\n'), _fp);
			return true;
		}
		else
		{
			ASSERT(FALSE);
			return false;
		}
	}

	void Close()
	{
		if (_fp)
		{
			fclose(_fp);
			_fp = NULL;
		}
	}

	LPCTSTR getErrorMsg() const { return _szError; }

protected:
	FILE* _fp;
	errno_t _result;
	TCHAR _szError[256];
};

template <typename T> class tArray
{
public:
	tArray() : _p(nullptr), size(0), count(0) {}
	~tArray() { delete[] _p; }

	bool setSize(int N)
	{
		if (0 < N)
		{
			if (_p)
				delete[] _p;
			_p = new T[N];
			memset(_p, 0, sizeof(T)*N);
			size = N;
			count = 0;
			return true;
		}
		return false;
	}

	bool add(T v)
	{
		if (size == count)
		{
			T* pt = new T[size+4];
			if (_p)
			{
				memcpy(pt, _p, sizeof(T)*count);
				delete[] _p;
			}
			size += 4;
			_p = pt;
		}
		_p[count++] = v;
		return true;
	}

	T& operator[](int index)
	{
		index = max(0, index);
		if (size <= index)
		{
			T* pt = new T[index+4];
			if (_p)
			{
				memcpy(pt, _p, sizeof(T)*count);
				delete[] _p;
			}
			size = index + 4;
			_p = pt;
		}
		if (count <= index)
			count = index + 1;
		return _p[index];
	}

protected:
	T* _p;
	int size, count;
};

typedef tArray<int> CIntArray;


//////////////////////////////////////////////////////////////////////////
typedef void(__stdcall *fLOGFUNC)(LPCTSTR pszLog);
typedef CTypedPtrList<CPtrList, CAM_CONTOUR*> CPtrContourList;

extern const TCHAR csz_RestartHead[];
extern const TCHAR csz_RestartTail[];
extern const TCHAR csz_TagOriginalPart[];
extern const TCHAR csz_TagModifiedPart[];
extern const TCHAR csz_ElemOrgPartNo[];
extern const TCHAR csz_ElemOrgContNo[];
extern const TCHAR csz_ElemOrgCodeIndex[];
extern const TCHAR csz_ElemBlockNo[];
extern const TCHAR csz_ElemReverse[];
extern const TCHAR csz_ElemLineNo[];
extern const TCHAR csz_ModContourLineNo[];
extern const TCHAR csz_ElemLineNoOffset[];
extern const TCHAR csz_RestartWCSx[];
extern const TCHAR csz_RestartWCSy[];
extern const TCHAR csz_RestartPartHead[];
extern const TCHAR csz_RestartPartTail[];

struct RESTART_LOG
{
	// The original part information where the past work stopped
	int  partNo;
	int  contourNo;
	int  GcodeIndex;
	int  shapeNBlockNo;	// without prefix 'N'
	double wcsX, wcsY;

	BOOL isReverse;

	// The modified part information
	int modShapeNBlockNo;// without prefix 'N'
	int modStartLine;	// the line number in MFP from which information about the restart (modified) part specified
	int modContourStartLine;	// HKSTR line number for the modified contour
	int offsetModLines;	// the number of lines for the modified part blocks (excluding the first mark-up line)

	int iRestartStart;
	int nRestartLogLines;

	RESTART_LOG() { clear(); }
	void clear() { memset(this, 0, sizeof(RESTART_LOG)); }
};

class FileMPF
{
public:
	FileMPF() : _version(verUnknown), _N1_lineNo(0), _iCurrLine(-1) {}
	~FileMPF() {}

	bool	read(LPCTSTR pszFile, TCHAR* pszError, size_t errBufCount);
	bool	copyTo(FileWriter& file);
	fileVersion	getVersion() const;
	LPCTSTR getFilename() const;
	LPCTSTR getFilePath() const;
	int		getN1LineNo() const;	// returns 1-based Post-program-start (block number N1) line number
	int		lines() const;			// total number of lines loaded from the MPF
	bool	isRestartFile() const;
	bool	isRestartReverse() const;
	bool	getCurrLine(CString& str);	// get the content of the current line
	bool	getNextBlock(CString& str);	// get a valid block string from the current line
	bool	getNextLine(CString& str);	// proceed to the next line and get its content
	bool	getLineText(int iLine, CString& str);		// iLine = zero-based index of text line array
	bool	setLineText(int iLine, const CString& str);	// iLine = zero-based index of text line array
	int		getCurrLineIndex() const;					// returns 0-based index of the current line to read
	int		getLineNoToRead() const;					// returns 1-based line number
	int		getLastLineNo() const;						// returns 1-based number of the line read just before
	bool	getWholeContent(CStringArray& arrLine);

	bool	rewind();
	bool	next();
	bool	back();
	bool	feed(CString& str);

	bool	write(int iLineTo, FileWriter& file);	// iLineTo = zero-based index of text line array
	bool	writeToEnd(FileWriter& file);
	bool	moveToLine(int iLineTo);				// iLineTo = zero-based index of text line array

	RESTART_LOG& getRestartInfo();

protected:
	// File data
	CStringArray _strLines;
	int			_iCurrLine;	// starts from 0
	// File information
	int			_N1_lineNo;	// starts from 1
	fileVersion	_version;
	CString		_strFilePath;
	CString		_strFileName;
	// Restart information, if exists
	RESTART_LOG restartLog;

	bool isValidIndex(int iLine) const;
	void setFilePath(LPCTSTR pszFile);
	bool getVersion(const CString& sLine);
	bool getRestartInfo(const int iRestart);
};

class CAMContainer;

//////////////////////////////////////////////////////////////////////////
// InterfaceMPF class
//
class InterfaceMPF
{
	// Constructors & destructor
public:
	InterfaceMPF();
	~InterfaceMPF();

	// File load helpers
	enum tokenType
	{
		tokenNull,
		tokenStateNo,
		tokenGcode,
		tokenMcode,
		tokenProcCall,
		tokenUnknown
	};

	// Operations
public:
	bool Load(LPCTSTR pszFile, CAMContainer& container);	// the master process of reading a MPF file

	// Static helpers for logging functionality
	static void SetErrorLogLevel(LogLevel level);
	static LPCTSTR GetLastErrorMessage();
	static void SetLogFunction(fLOGFUNC f);
	static fLOGFUNC GetLogFunction();
	static void PrintLog(LogLevel logLevel, LPCTSTR pszFormat, ...);
	// Static helpers for cleaning contents or adding supplementary informationile);
	static bool SaveStrListToFile(CStringList& strList, LPCTSTR pszSavePath);

	// Implementation
protected:
	static LogLevel s_logErrLevel;
	static TCHAR s_szError[1024];
	static fLOGFUNC SaveLogMessage;
	static tokenType GetToken(CString& str, int& nBlockNo, CString& strProc, CStringList& strArgs);
	static void GetCodeArgs(const CString& strLine, int iPos, CStringList& strArgs);
	static void GetProcArgs(const CString& strLine, int iPos, CStringList& strArgs);

	static void MakeString(CString& str);

	int _numPartsTotal;
	CAM_PART* _cpPartInfo;

	bool LoadV08(FileMPF& loader, CAM_DATA& camInfo);
	bool LoadV16(FileMPF& loader, CAM_DATA& camInfo);
	bool LoadV16A05(FileMPF& loader, CAM_DATA& camInfo);

	// Helpers for nesting layout
	bool UpdateCAMPartInfo(const CPtrContourList& ptrListContour, int nLineNo, CAM_DATA& camInfo, int& iPart);
	void GetHKLDBv08(const CStringList& strArgList, CAM_DATA& camInfo);
	void GetHKLDBv16(const CStringList& strArgList, CAM_DATA& camInfo);
	void GetHKINIv08(const CStringList& strArgList, CAM_DATA& camInfo);
	void GetHKINIv16(const CStringList& strArgList, CAM_DATA& camInfo);
	bool GetHKOSTv08(int nLineNo, const CString& strProc, const CStringList& strArgList, CStringList& strList_HKOST);
	bool GetHKOSTv16(int nLineNo, const CString& strProc, const CStringList& strArgList, CStringList& strList_HKOST);
	bool GetHKOSTPartInfo(fileVersion verMPF, const CString& strArgs, CAM_PART& info);

	// Part nesting information parser
	bool ReadHKLDB(FileMPF& loader, CAM_DATA& camInfo);	// handles HKLDB cutting DB sub-routine call
	bool ReadHKINI(FileMPF& loader, CAM_DATA& camInfo);	// handles HKINI initialization sub-routine call
	bool ReadHKOSTv08(FileMPF& loader, CStringList& strList_HKOST, bool& bPartsFinished);	// part origin
	bool ReadHKOSTv16(FileMPF& loader, CStringList& strList_HKOST, bool& bPartsFinished);	// part origin
	bool ReadProcEndMcode(FileMPF& loader, int& numShapes);	// finds the end of the part nesting information while getting the number of following part shapes
	bool InitCAMPartBuf(fileVersion version, const CStringList& strHKOSTList, CAM_DATA& camInfo, const int numPartShapes);	// used in preparing CAM part buffer while reading the CAM part information

	// v08 specific functions
	// CAM object information parser
	bool ReadHKLON(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd);	// finds the next HKLON and gets its information
	bool ReadHKTON(FileMPF& loader, CAM_CONTOUR& contour);		// finds the next HKTON to proceed to getting G-code list
	bool FindHKTOF(FileMPF& loader, CAM_CONTOUR& contour);		// reads G-codes until finds HKTOF
	bool FindHKLOF(FileMPF& loader, bool& bReachEnd);			// finds the closing HKLOF while checking if the current snippet contains HKPED(=the end of CAM part)
	bool ReadHKSCR(CStringList& strArgs, FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd);	// finds remnant handling call
	bool ReadThroughHKLOF(FileMPF& loader);	// skips to the next HKLON-HKLOF snippet when it meets unrecognizable HKLON-HKLOF snippet (unreferenced part number)
	bool GetHKLON(int nLineNo, const CStringList& strArgs, CAM_CONTOUR& contour);
	bool GetHKTOFArgsGcmd(int nLineNo, const CStringList& strHKTOFAgrs, CString& strGcommand);

	// v16 CAM object information parser
	bool ReadContour(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd, bool& bPartCompleted);	// finds the next HKLON and gets its information
	bool ReadContourV16A05(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd, bool& bPartCompleted);	// finds the next HKLON and gets its information
	bool ReadHKSTR(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd, bool& bPartCompleted);	// finds the next HKLON and gets its information
	bool ReadHKSCRC(CStringList& strArgs, FileMPF& loader, CAM_CONTOUR& contour);	// finds remnant handling call
	bool ReadHKSCRCV16A05(CStringList& strArgs, FileMPF& loader, CAM_CONTOUR& contour, bool& bPartCompleted);	// finds remnant handling call
	bool GetHKSTR(int nLineNo, const CStringList& strArgs, CAM_CONTOUR& contour);
	bool GetHKLEAV16A05Args(CString& str, CStringList& strArgs, CStringList& strGcodeList, CStringList& strBlockList, CAM_CONTOUR& contour);
	bool GetHKCUTV16A05Args(CString& str, CStringList& strArgs, CAM_CONTOUR& contour);
	bool GetHKSTOV16A05Args(CString& str, CStringList& strArgs, CStringList& strGcodeList, CStringList& strBlockList, CAM_CONTOUR& contour);
	bool GetHKSCRC_HKSTOArgs(CString& str, CStringList& strArgs, CStringList& strGcodeList, CStringList& strBlockList, CAM_CONTOUR& contour);

	// Parsing helpers common to both of v08 and v16
	G_Code GetContourParamsInArg(const CStringList& strArgs, CString& strGcode);
	bool AddGcodeBlockToList(int nLineNo, const CString& strGcode, const CStringList& strCoordsList, CStringList& strGcodeList);
	bool CheckGcodeBlock(int nLineNo, const CString& strGcode, const CStringList& strCoordsList);
	bool G_LineCoords(int nLineNo, const CStringList& strListArgs, CString& strGcode);
	bool G_ArcCoords(int nLineNo, const CStringList& strListArgs, CString& strGcode);
	bool BuildContourPath(const CStringList& strGcmdList, const CStringList& strBlockList, CIntArray& arrLineNo, CAM_CONTOUR& contour);
	bool BuildContourPath(FileMPF& loader, const CStringList& strGcmdList, const CStringList& strBlockList, CIntArray& arrLineNo, CAM_CONTOUR& contour, bool& bPartCompleted);
	bool AddToContourList(CPtrContourList& list, CAM_CONTOUR& contour);
	void DeleteContourList(CPtrContourList& list);

	bool GetGCodeLastPos(const CString& strGcode, const CStringList& strCoordsList, CPtrList& elemList);
};


//////////////////////////////////////////////////////////////////////////

struct CAM_PART
{
	// Position information relative to the workpiece origin
	//
	double origin_X;	// part's x-offset from workpiece origin
	double origin_Y;	// part's y-offset from workpiece origin
	double origin_R;	// part's rotation angle relative to workpiece coordinate system

	// CAM_PART reference in nesting
	// The statement number in MPF file and array index in CAM_PART buffer of the current part in nesting
	int nCAMpart_BlockNo;	// program block statement number of CAM object start position
	int iCAMShape;			// index of CAM object in CAM object array of CAM_PART
	int nContoursInPart;	// number of contours in CAM object

	int iCmdBlockStr;		// block command string index in the source file
	int nDetours;			// number of detour codes
	CAM_CODE* pDetour;

	CAM_PART()
	{
		origin_X = DBL_MAX, origin_Y = DBL_MAX, origin_R = DBL_MAX;
		nCAMpart_BlockNo = -1, nContoursInPart = 0;
		iCmdBlockStr = -1, nDetours = 0;
		pDetour = NULL;
	}
	~CAM_PART();
};

struct CAM_CODE
{
	G_Code code;
	double X, Y, I, J, F;
	int nLineNo;		// the line number of the G-code in the file
	CString sBlockCmd;	// the original block statement in the file

	CAM_CODE() : code(G_Unknown), X(DBL_MAX), Y(DBL_MAX), I(DBL_MAX), J(DBL_MAX), F(DBL_MAX), nLineNo(0) {}
	bool	FromString(const CString& strGcmd);
	bool	IsArc() const { return (G_ArcCW==code||G_ArcCCW==code); }
	double	CalcLength(double start_x, double start_y) const;
	bool	CalcPos(double start_x, double start_y, double ratioFromStart, double& x, double& y, double& newI, double& newJ, double offset = 0) const;

	const CAM_CODE& operator=(const CAM_CODE& other)
	{
		if (this != &other)
		{
			code = other.code;
			X = other.X;
			Y = other.Y;
			I = other.I;
			J = other.J;
			F = other.F;
			nLineNo = other.nLineNo;
			sBlockCmd = other.sBlockCmd;
		}
		return *this;
	}
};

struct CAM_CONTOUR
{
	// block ID
	int	N_blockNo;	// contour block number

	// geometrical attributes
	double	x_start, y_start, u_start;	// contour start coordinates
	double	width, height;				// contour dimension

	// processing attributes
	int nPiercing, nCutting, nToolComp;

	// Remnant cutting flag
	bool bHasLeadIn, bIsRemnant;

	// Scan cut flag
	bool bIsScancut;

	// contour components
	int			numCodes;
	CAM_CODE*	pCode;

	// detour information, if exists
	int			iCmdBlockStr;	// block command string index in the source file
	int			num_detours;
	CAM_CODE*	pDetour;
	int			iCmdBlockStrLast; // block command string index of the last block

	// operations
	CAM_CONTOUR() { memset(this, 0, sizeof(CAM_CONTOUR));  iCmdBlockStr= iCmdBlockStrLast = -1; }
	~CAM_CONTOUR() { Delete(); }
	bool HasPiercing() const { return (0 < nPiercing); }
	bool IsEmpty() const { return (0==numCodes && NULL == pCode); }
	void Delete() { delete[] pCode; delete[] pDetour; numCodes = 0; pCode = NULL; pDetour = NULL; }
	bool GetEndPos(double& x, double& y) const;

private:
	CAM_CONTOUR(CAM_CONTOUR& c);
	void operator=(CAM_CONTOUR& c);
	friend class InterfaceMPF;
};

struct CAM_SHAPE
{
	int nBlockIdNo;
	int	numContours;
	int	nEndOfPartLineNo;
	CAM_CONTOUR* pContour;

	CAM_SHAPE() { memset(this, 0, sizeof(CAM_SHAPE)); }
	~CAM_SHAPE() { delete[] pContour; }
	bool IsValid() const { return (10001 <= nBlockIdNo && 0 < numContours && AfxIsValidAddress(pContour, sizeof(CAM_CONTOUR)*numContours)); }
};

class CAM_DATA
{
public:
	fileVersion version;
	CString strFileName;

	CString strDBName;
	CString strMaterial;
	int		nMaterialThickness;
	CString strAssistGas;

	// Sheet attributes
	double sheetWidth, sheetHeight;

	// Geometrical & processing information
	//	CAM part nesting information
	int	num_Parts;
	CAM_PART* pPart;
	//	CAM object geometrical information
	int	num_Shapes;
	CAM_SHAPE* pCAMShape;

	CAM_DATA();
	~CAM_DATA();

	bool HasContents() const { return (0 < num_Shapes && AfxIsValidAddress(pCAMShape, sizeof(CAM_SHAPE)*num_Shapes)); }
	bool DumpContents(LPCTSTR pszFile) const;
	bool IsNCPart(int nBlockNo) const;
	void Delete();
	void GetDBInfo(CString& str) const;
	void GetVersion(CString& strVersion) const;

protected:
	friend class CAMContainer;

	// Warning:
	// Those protected functions below
	//    must be called upon checking the validity of CAM_PART and CAM_SHAPE buffers
	const CAM_PART& firstNesting() const { return pPart[0]; }
	const CAM_PART& lastNesting() const { return pPart[num_Parts-1]; }
	int nestingStartLine() const { return firstNesting().iCmdBlockStr + 1; }
	int nestingEndLine() const { return (verV08==version)? lastNesting().iCmdBlockStr+1: lastNesting().iCmdBlockStr+2; }
	const CAM_SHAPE& firstPart() const { return pCAMShape[0]; }
	const CAM_SHAPE& lastPart() const { return pCAMShape[num_Shapes-1]; }
	int partStartLine() const { return firstPart().pContour[0].iCmdBlockStr + 1; }
	int partEndLine() const { return lastPart().pContour[lastPart().numContours-1].iCmdBlockStrLast + 2; }	// including HKPED() line
};


//////////////////////////////////////////////////////////////////////////

inline fileVersion FileMPF::getVersion() const
{
	return _version;
}

inline LPCTSTR FileMPF::getFilename() const
{
	return _strFileName;
}

inline LPCTSTR FileMPF::getFilePath() const
{
	return _strFilePath;
}

inline int FileMPF::getN1LineNo() const
{
	return _N1_lineNo;
}

inline int FileMPF::lines() const
{
	return static_cast<int>(_strLines.GetCount());
}

inline bool FileMPF::isRestartFile() const
{
	return (0 < restartLog.partNo);
}

inline bool FileMPF::isRestartReverse() const
{
	return (FALSE != restartLog.isReverse);
}

inline int FileMPF::getLineNoToRead() const
{
	return (_iCurrLine + 1);
}

inline int FileMPF::getLastLineNo() const
{
	return _iCurrLine;
}

inline int FileMPF::getCurrLineIndex() const
{
	return _iCurrLine;
}

inline RESTART_LOG& FileMPF::getRestartInfo()
{
	return restartLog;
}

inline bool FileMPF::copyTo(FileWriter& file)
{
	return file.WriteStrings(_strLines);
}

inline bool FileMPF::isValidIndex(int iLine) const
{
	return (0 <= iLine && iLine < _strLines.GetCount());
}

inline bool InterfaceMPF::AddToContourList(CPtrContourList& list, CAM_CONTOUR& contour)
{
	CAM_CONTOUR* p = new CAM_CONTOUR(contour);
	if (!p)
	{
		PrintLog(enLogError, _T("*** Error: out of memory while allocating contour buffer"));
		DeleteContourList(list);
		return false;
	}
	ASSERT(contour.IsEmpty());
	list.AddTail(p);
	return true;
}

inline void InterfaceMPF::DeleteContourList(CPtrContourList& list)
{
	POSITION pos = list.GetHeadPosition();
	while (pos)
		delete list.GetNext(pos);
	list.RemoveAll();
	return;
}

inline bool IsCommentLine(const CString& sLine)
{
	const int nLength = sLine.GetLength();
	int i = 0;
	while (i < nLength && (_T(' ') == sLine[i] || _T('\t') == sLine[i]))
		++i;
	return (nLength <= i)? false: (_T(';') == sLine[i]);
}
