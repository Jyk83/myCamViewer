//H 헤더 파일 내용
#pragma once

#include "float.h"	// 실수 처리
#include "Graph2d.h"	// 2d 그래프 처리

enum fileVersion { verUnknown, verV08, verV16, verV16A05 }; // MPF의 버전 정의

enum G_Code // G Code 분별 코드 번호
{
	G_Jump,        // G0	
	G_Line,          // G1	
	G_ArcCW,      // G2	
	G_ArcCCW,    // G3	
	G_Unknown,
};

LPCTSTR getPiercingTypeName(int iPiercing);	// 16비트 유니코드 문자
LPCTSTR getCuttingTypeName(int iCutting); // 16비트 유니코드 문자
LPCTSTR getFileVersionName(fileVersion version); // 16비트 유니코드 문자

#define PIERCING_NONE			0	// V08
#define PIERCING_PULSE			1	// 피어싱 타입 분류
#define PIERCING_CW				2	
#define PIERCING_DOWN			3	
#define PIERCING_MP				4	
#define PIERCING_QUICK			5	
#define PIERCING_SHOTMARKING	8	
#define PIERCING_RAPID			9	

#define CUTTING_UNDEFINED		-1  // V08
#define CUTTING_SHOTMARKING		0	// 절단 타입 분류
#define CUTTING_HIGHSPEED		1	
#define CUTTING_MIDDLESPEED		2	
#define CUTTING_LOWSPEED		3	
#define CUTTING_SPECIAL			4	
#define CUTTING_ENGRAVEMARKING  11	

#define PIERCING_V16_NONE			0	// V16
#define PIERCING_V16_NORMAL			1	// 피어싱 타입 분류
#define PIERCING_V16_SHOTMARKING	10	
#define PIERCING_V16_REPEAT			11	

#define CUTTING_V16_NORMAL		1	// V16
#define CUTTING_V16_PULSE		2	// 절단 타입 분류
#define CUTTING_V16_MARKING		10	
#define CUTTING_V16_REPEAT		11	

class CAM_DATA;	// class CAM_DATA 정의
struct CAM_CODE;	// CAM_Code 구조체 정의
struct CAM_CONTOUR;	// CAM Contour 구조체 정의
struct CAM_SHAPE;	// CAM_Shape 구조체 정의
struct CAM_PART;	// CAM_Part 구조체 정의

enum LogLevel
{
	enLogError = 0x01,
	enLogWarning = 0x03,
	enLogInfo = 0x07,
};

//////////////////////////////////////////////////////////////////////////	

class FileWriter
{
public:
	FileWriter() : _fp(0) { _szError[0] = 0; }  // File point _fp 생성 및 szError 초기화
	~FileWriter() { if (_fp) fclose(_fp); }     // ~소멸자

	bool Open(LPCTSTR pszFile, bool isAppendingToExsting = false) // pszFile 명으로 File Open 하는 함수
	{
		if (_fp) // 기존 _fp 포인트가 참이면 close 후 NULL로 초기화
		{
			fclose(_fp);
			_fp = NULL;
		}

		LPCTSTR szMode = isAppendingToExsting ? _T("a+") : _T("w"); // 16비트 문자열 szMode에 isApeendingToExsting 에 따라 모드 설정 a+(append/update 내용 추가 및 새로만들기), w(Write-새로 빈 파일 만들기) 삽입
		_result = _tfopen_s(&_fp, pszFile, szMode); // _result 로그에 파일 오픈 하고 결과 값 저장 (0: 정상 그외 : 비정상)
		
		if (_result)
		{
			_tcserror_s(_szError, _result);
			return false;
		}
		return true;
	}

	bool WriteString(LPCTSTR pszLine) // 파일에 문자열 삽입
	{
		if (_fp)
		{
			return (EOF != _fputts(pszLine, _fp)); // 파일의 끝이 아닐 때 내용 삽입 //fputtts TCHAR 형식으로 문자열 복사
		}
		else
		{
			ASSERT(FALSE);
			return false;
		}
	}

	bool WriteStrings(CStringArray& fileText) // 파일의 문자행 삽입
	{
		if (_fp)
		{
			for (int i = 0; i < fileText.GetCount(); ++i) // fileText의 행 만큼 반복
				_fputts(fileText.GetAt(i) + _T('\n'), _fp); // fileText 해당 행의 문자열을 복사하여 줄바꿈 ('\n') 삽입 후 _fp에 TCHAR 형식으로 저장
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

	LPCTSTR getErrorMSG() const { return _szError; } // 16비트 유니코드 문자로 에러 메시지를 리턴한다.

protected:
	FILE* _fp;
	errno_t _result;
	TCHAR _szError[256]; // 16비트 유니코드 문자
};

template <typename T> class tArray1 // 아 뭐라냐... 클래스 또는 함수의 연산을 정의할 수 있다. 함수에 필요한 변수로 변환 된다.
{
public:
	tArray1() : _p(nullptr), size(0), count(0) {} // tArray() 함수 초기화
	~tArray1() { delete[] _p; }

	bool setSize(int N) // 매개 변수 N에 의하여 size를 설정
	{
		if (0 < N)
		{
			if (_p)
				delete[] _p; // 기존 _p 삭제 및 초기화
			_p = new T[N]; // N개 만큼 Template 생성 및 _p 전달
			memset(_p, 0, sizeof(T)*N); // _p의 메모리 초기화 N사이즈 만큼 Template를 0로 초기화
			size = N; // size 넘버
			count = 0; // count 0 설정
			return true;
		}
		return false;
	}

	bool add(T v) // Template 매개 변수 v 추가
	{
		if (size == count) // size만큼 카운트 증가하면!
		{
			T* pt = new T[size + 4]; // template pt에 T[배열 사이즈 = size + 4] 생성
			if (_p)
			{
				memcpy(pt, _p, sizeof(T)*count); // pt에 _p를 Template * count의 메모리 만큼 복사
				delete[] _p; // _p 내용 삭제
			}
			size += 4; // size + 4 증분
			_p = pt; // _p에는 기존 pt 내용 대입
		}
		_p[count++] = v; //_p[카운트 증가]에 V template 추가
		return true;
	}

	T& operator[](int index) // operater[] 배열 연산 및 증분
	{
		index = max(0, index); // index 0 이하 값인지 판별 후 리턴
		if (size <= index)
		{
			T* pt = new T[index + 4]; // Template 포인트 pt에 [index + 4] 사이즈 만큼의 Template 할당
			if (_p)
			{
				memcpy(pt, _p, sizeof(T)*count); // pt에 _p 포인트의 count 사이즈 만큼 메모리 복사
				delete[] _p; // _p 메모리 삭제
			}
			size = index + 4; // size에 index + 4 대입
			_p = pt; // _p 포인트에는 pt 새로 삽입
		}
		if (count <= index) // count가 index보다 작은 경우 1씩 증가
			count = index + 1;
		return _p[index];
	}

protected:
	T* _p; // T template 포인트 할당
	int size, count; // 사이즈 및 카운터 변수 할당
};

typedef tArray<int> CIntArray; // tArray의(t=template) Int 매개변수 형으로 CIntArray 타입 재정의

typedef void(__stdcall *fLOGFUNC)(LPCTSTR pszLog); // stdcall 하는 fLogFunc 포인트 지정, 16비트 유니코드형 pszLog 할당
typedef CTypedPtrList<CPtrList, CAM_CONTOUR*> CPtrContourList; // CTypedPtrList< CPtrList(파트 리스트), CAM_Contour*(컨투어 포인터)를 할당하는 CPtrContourList를 생성

//16비트 유티코드형 Char csz_RestartHead[] 배열을 전역변수 및 상수로 생성 https://blog.naver.com/kut_da_92/222885136954
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
	// 중단된 파트 컨투어 작업 정보
	int partNo;
	int contourNo;
	int GcodeIndex;
	int shapeNBlockNo; // N 넘버 없이
	double wcsX, wcsY;

	BOOL isReverse;

	int modShapeNBlockNo;
	int modStartLine; // 재시작 부분에 대한 정보가 지정된 MPF의 줄번호
	int modContourStartLine; // 수정된 컨투어의 HKSTR라인 번호
	int offsetModeLines; // 수정된 부분 블록의 줄 수

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

	bool read(LPCTSTR pszFile, TCHAR* pszError, size_t errBufCount);
	bool copyTo(FileWriter& file);
	fileVersion getVersion() const;
	LPCTSTR getFilename() const;
	LPCTSTR getFilePath() const;
	int getN1LineNo() const;
	int lines() const;
	bool isRestartFile() const;
	bool isRestartReverse() const;
	bool getCurrLine(CString& str);
	bool getNextBloc(CString& str);
	bool getNextLine(CString& str);
	bool getLineText(int iLine, CString& str);
	bool getLineText(int iLine, const CString& str);
	int getCurrLineIndex() const;
	int getLineNoToRead() const;
	int getLastLineNo() const;
	bool getWholeContent(CStringArray& arrLine);

	bool rewind();
	bool next();
	bool back();
	bool feed(CString& str);

	bool write(int iLineTo, FileWriter& file);
	bool writeToEnd(FileWriter& file);
	bool moveToLine(int iLineTo);

	RESTART_LOG& getRestartInfo();

protected:
	CStringArray _strLines;
	int _iCurrLine;
	int _N1_lineNo;
	fileVersion _version;
	CString _strFilePath;
	CString _strFileName;
	RESTART_LOG restartLog;

	bool isValidIndex(int iLine) const;
	void setFilePath(LPCTSTR pszFile);
	bool getVersion(const CString& sLine);
	bool getRestartInfo(const int iRestart);
};

class CAMContainer;

class InterfaceMPF
{
public:
	InterfaceMPF();
	~InterfaceMPF();

	enum tokenType
	{
		tokenNull,
		tokenStateNo,
		tokenGcode,
		tokenMcode,
		tokenProcCall,
		tokenUnknown
	};

public:
	bool Load(LPCTSTR pszFile, CAMContainer& container);

	static void SetErrorLogLevel(LogLevel level);
	static LPCTSTR GetLastErrorMessage();
	static void SetLogFunction(fLOGFUNC f);
	static fLOGFUNC GetLogFunction();
	static void PrintLog(LogLevel logLevel, LPCTSTR pszFormat, ...);
	static bool SaveStrListToFile(CStringList& strList, LPCTSTR pszSavePath);

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

	bool UpdateCAMPartInfo(const CPtrContourList& ptrListContour, int nLineNo, CAM_DATA& camInfo, int& iPart);
	void GetHKLDBv08(const CStringList& strArgList, CAM_DATA& camInfo);
	void GetHKLDBv16(const CStringList& strArgList, CAM_DATA& camInfo);
	void GetHKINIv08(const CStringList& strArgList, CAM_DATA& camInfo);
	void GetHKINIv16(const CStringList& strArgList, CAM_DATA& camInfo);
	bool GetHKOSTv08(int nLineNo, const CString& strProc, const CStringList& strArgList, CStringList& strList_HKOST);
	bool GetHKOSTv16(int nLineNo, const CString& strProc, const CStringList& strArgList, CStringList& strList_HKOST);
	bool GetHKOSTPartInfo(fileVersion verMPF, const CString& strArgs, CAM_PART& info);

	bool ReadHKLDB(FileMPF& loader, CAM_DATA& camInfo);
	bool ReadHKINI(FileMPF& loader, CAM_DATA& camInfo);
	bool ReadHKOSTv08(FileMPF& loader, CStringList& strList_HKOST, bool& bPartsFinished);
	bool ReadHKOSTv16(FileMPF& loader, CStringList& strList_HKOST, bool& bPartsFinished);
	bool ReadProcEndMcode(FileMPF& loader, int& numShapes);
	bool InitCAMPartBuf(fileVersion version, const CStringList& strHKOSTList, CAM_DATA& camInfo, const int numPartShapes);

	bool ReadHKLON(FileMPF& loader, CAM_CONTOUR& contour, bool& bReadEnd);
	bool ReadHKTON(FileMPF& loader, CAM_CONTOUR& contour);
	bool FindHKTOF(FileMPF& loader, CAM_CONTOUR& contour);
	bool FindHKLOF(FileMPF& loader, bool& bReachEnd);
	bool ReadHKSCR(CStringList& strArgs, FileMPF& loader, CAM_CONTOUR& contour, bool& bReadEnd);
	bool ReadThroughHKLOF(FileMPF& loader);
	bool GetHKLON(int nLineNo, const CStringList& strArgs, CAM_CONTOUR& contour);
	bool GetHKTOFArgsGcmd(int nLintNo, const CStringList& strHKTOFArgs, CString& strGcommand);

	bool ReadContour(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd, bool& bPartCompleted);
	bool ReadContourV16A05(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd, bool& bPartCompleted);
	bool ReadHKSTR(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd, bool& bPartCompleted);
	bool ReadHKSCRC(CStringList& strArgs, FileMPF& loader, CAM_CONTOUR& contour);
	bool ReadHKSCRCV16A05(CStringList& strArgs, FileMPF& loader, CAM_CONTOUR& contour, bool& bPartCompleted);
	bool GetHKSTR(int nLineNo, const CStringList& strArgs, CAM_CONTOUR& contour);
	bool GetHKLEAV16A05Args(CString& str, CStringList& strArgs, CStringList& strGcodeList, CStringList& strBlockList, CAM_CONTOUR& contour);
	bool GetHKCUTV16A06Args(CString& str, CStringList& strArgs, CAM_CONTOUR& contour);
	bool GetHKSTOV16A05Args(CString& str, CStringList& strArgs, CStringList& strGcodeList, CStringList& strBlockLst, CAM_CONTOUR& contour);
	bool GetHKSCRC_HKSTOArgs(CString& str, CStringList& strArgs, CStringList& strGcodeList, CStringList& strBlockLst, CAM_CONTOUR& contour);

	G_Code GetContourParamsInArg(const CStringList& strArgs, CString& strGcode);
	bool AddGcodeBlockTolist(int nLineNo, const CString& strGcode, const CStringList& strCoordsList, CStringList& strGcodeList);
	bool CheckGcodeBlock(int nLineNo, const CString& strGcode, const CStringList& strCoordsList);
	bool G_LineCoords(int nLineNo, const CStringList& strListArgs, CString& strGcode);
	bool G_ArcCoords(int nLineNo, const CStringList& strListArgs, CString& strGcode);
	bool BuildContourPath(const CStringList& strGcmdList, const CStringList& strBlockList, CIntArray& arrLineNo, CAM_CONTOUR& contour);
	bool BuildContourPath(FileMPF& loader, const CStringList& strGcmdList, const CStringList& strBlockList, CIntArray& arrLineNo, CAM_CONTOUR& contour, bool& bPartCompleted);
	bool AddToContourList(CPtrContourList& list, CAM_CONTOUR& contour);
	void DeleteContourList(CPtrContourList& list);

	bool GetGCodeLastPos(const CString& strGcode, const CStringList& strCoordsList, CPtrList& elemList);
};

struct CAM_PART
{
	double origin_X;
	double origin_Y;
	double origin_R;

	int nCAMpart_BlockNo;
	int iCAMShape;
	int nContoursInPart;

	int iCmdBlockStr;
	int nDetours;
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
	int nLineNo;
	CString sBlockCmd;

	CAM_CODE() : code(G_Unknown), X(DBL_MAX), Y(DBL_MAX), I(DBL_MAX), J(DBL_MAX), F(DBL_MAX), nLineNo(0) {}
	bool FromString(const CString& strGcmd);
	bool IsArc() const { return (G_ArcCW == code || G_ArcCCW == code); }
	double CalcLength(double start_x, double start_y) const;
	bool CalcPos(double start_x, double start_y, double ratioFromStart, double& x, double& y, double& newI, double& newJ, double offset = 0) const;

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
	int N_blockNo;
	double x_start, y_start, u_start;
	double width, height;
	int nPiercing, nCutting, nToolComp;
	bool bHasLeadIn, bIsRemnant;
	bool blsScancut;
	int numCodes;
	CAM_CODE* pCode;
	int iCmdBlockStr;
	int num_detours;
	CAM_CODE* pDetour;
	int iCmdBlockStrLast;

	CAM_CONTOUR() { memset(this, 0, sizeof(CAM_CONTOUR)); iCmdBlockStr = iCmdBlockStrLast = -1; }
	~CAM_CONTOUR() { Delete(); }

	bool HasPiercing() const { return (0 < nPiercing); }
	bool IsEmpty() const { return (0 == numCodes && NULL == pCode); }
	void Delete() { delete[] pCode; delete[] pDetour; numCodes = 0; pCode = NULL; pDetour = NULL; }
	bool GetEndPos(double& x, double& y) const;

private:
	CAM_CONTOUR(CAM_CONTOUR& c);
	void operator = (CAM_CONTOUR& c);
	friend class InterfaceMPF;
};

struct CAM_SHAPE
{
	int nBlockNo;
	int numContours;
	int nEndOfPartLineNo;
	CAM_CONTOUR* pContour;

	CAM_SHAPE() { memset(this, 0, sizeof(CAM_SHAPE)); }
	~CAM_SHAPE() { delete[] pContour; }
	bool IsValid() const { return (10001 <= nBlockNo && 0 < numContours && AfxIsValidAddress(pContour, sizeof(CAM_CONTOUR)*numContours)); }
};

class CAM_DATA
{
public:
	fileVersion version;
	CString strFileName;

	CString strDBName;
	CString strMaterial;
	int nMaterialThickness;
	CString strAssistGas;

	double sheetWidth, sheetHeight;

	int num_Parts;
	CAM_PART* pPart;
	int num_Shapes;
	CAM_SHAPE* pCAMShape;

	CAM_DATA();
	~CAM_DATA();

	bool HasContents() const { return (0 < num_Shapes && AfxIsValidAddress(pCAMShape, sizeof(CAM_SHAPE)*num_Shapes)); }
	bool Du9mpContents(LPCTSTR pszFile) const;
	bool IsNCPart(int nBlockNo) const;
	void Delete();
	void GetDBInfo(CString& str) const;
	void GetVersion(CString& strVersion) const;

protected:
	friend class CAMContainer;

	const CAM_PART& firstNesting() const { return pPart[0]; }
	const CAM_PART& lastNesting() const { return pPart[num_Parts - 1]; }
	int nestingStartLine() const { return firstNesting().iCmdBlockStr + 1; }
	int nestingEndLine() const { return (verV08 == version) ? lastNesting().iCmdBlockStr + 1 : lastNesting().iCmdBlockStr + 2; }
	const CAM_SHAPE& firstPart() const{ return pCAMShape[0]; }
	const CAM_SHAPE& lastPart() const { return pCAMShape[num_Shapes - 1]; }
	int partStartLine() const { return firstPart().pContour[0].iCmdBlockStr + 1; }
	int partEndLine() const { return lastPart().pContour[lastPart().numContours - 1].iCmdBlockStrLast + 2; }
};

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
		PrintLog(enLogError, _T("*** Error : out of memory while allocating contour buffer"));
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
	while (i < nLength && (_T('') == sLine[i] || _T('\t') == sLine[i]))
		++i;
	return (nLength <= i) ? false : (_T(';') == sLine[i]);
}