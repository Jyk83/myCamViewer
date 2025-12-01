#include "stdafx.h"
#include "CAMViewerFactory.h"
#include "MPFInterface.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif // _DEBUG

#pragma warning(disable : 4996)

//////////////////////////////////////////////////////////////////////////

CString CAMViewerFactory::s_logFolderRestart;
TCHAR s_szLogBuf[4096];


//////////////////////////////////////////////////////////////////////////
// CAMViewerFactory and CAMContainer helpers

int ErrCode_PROF2CAM(int code)
{
	switch (code)
	{
	case PROFILE_SUCCESS:				return CV_NOERROR;	break;
	case PROFILE_ERR_MEMORY:			return CVERR_OUTOFMEMORY; break;
	case PROFILE_INVALID_INPUT:			return CVERR_INVALID_ARGS; break;
	case PROFILE_ERR_NOT_FLYINGMODE:	return CVERR_FLYING_NOTAPPLICABLE; break;
	case PROFILE_ERR_INPUT_JERK_X:		return CVERR_FLYING_JERK_X; break;
	case PROFILE_ERR_INPUT_ACCL_X:		return CVERR_FLYING_ACCL_X; break;
	case PROFILE_ERR_INPUT_FEED_X:		return CVERR_FLYING_FEED_X; break;
	case PROFILE_ERR_INPUT_JERK_Y:		return CVERR_FLYING_JERK_Y; break;
	case PROFILE_ERR_INPUT_ACCL_Y:		return CVERR_FLYING_ACCL_Y; break;
	case PROFILE_ERR_INPUT_FEED_Y:		return CVERR_FLYING_FEED_Y; break;
	case PROFILE_ERR_INPUT_JERK_Z:		return CVERR_FLYING_JERK_Z; break;
	case PROFILE_ERR_INPUT_ACCL_Z:		return CVERR_FLYING_ACCL_Z; break;
	case PROFILE_ERR_INPUT_FEED_Z:		return CVERR_FLYING_FEED_Z; break;
	case PROFILE_ERR_INPUT_DISTANCE:	return CVERR_FLYING_DISTANCE; break;
	case PROFILE_ERR_INPUT_TIME:		return CVERR_FLYING_TIME; break;
	case PROFILE_ERR_INPUT_FEEDS:		return CVERR_FLYING_FEEDS; break;
	case PROFILE_ERR_NOT_INITIALIZED:	return CVERR_FLYING_NOT_READY; break;
	default: break;
	}
	return CVERR_UNKNOWN;
}

int trimZeroTail(CString& str)
{
	int iDot = str.Find(_T('.'));
	CString strInteger = str.Left(iDot);
	CString strFraction = str.Mid(iDot+1);

	int i = strFraction.GetLength();
	for (--i; 0 <= i; --i)
	{
		if (_T('0') != strFraction[i])
		{
			if (strFraction.GetLength()-1 > i)
				strFraction = strFraction.Left(i+1);
			break;
		}
	}
	if (0 > i)
	{
		strFraction.Empty();
		str = strInteger + _T('.');
	}
	else
	{
		str = strInteger + _T('.') + strFraction;
	}

	return strFraction.GetLength();
}

void getFloatString(double x, double y, bool inches, CString& str)
{
	CString strX, strY;
	LPCTSTR pszFormat = inches? _T("%.5f"): _T("%.3f");

	strX.Format(pszFormat, x);
	strY.Format(pszFormat, y);
	trimZeroTail(strX);
	trimZeroTail(strY);
	str.Format(_T("%s,%s"), strX, strY);
}

void getFloatString(double x, double y, double r, bool inches, CString& str)
{
	CString strX, strY, strR;
	LPCTSTR pszFormat = inches? _T("%.5f"): _T("%.3f");

	strX.Format(pszFormat, x);
	strY.Format(pszFormat, y);
	strR.Format(pszFormat, r);
	trimZeroTail(strX);
	trimZeroTail(strY);
	trimZeroTail(strR);
	str.Format(_T("%s,%s,%s"), strX, strY, strR);
}

void getJumpFloatString(double hUp, double hDown, double residual, bool inches, CString& str, int& nJumpType)
{
	CString strZup, strZdn, strRes;
	LPCTSTR pszFormat = inches? _T("%.5f"): _T("%.3f");

	strZup.Format(pszFormat, hUp);
	strZdn.Format(pszFormat, hDown);
	strRes.Format(pszFormat, residual);

	trimZeroTail(strZup);
	trimZeroTail(strZdn);
	if (IsZeroStr(strZdn))
	{
		strRes = _T("0.");
		if (nJUMPTYPE_FLYING == nJumpType)
		{
			nJumpType = (IsZeroStr(strZup))? nJUMPTYPE_NONE: nJUMPTYPE_HALFFLYING;
		}
	}
	else
	{
		trimZeroTail(strRes);
	}
	str.Format(_T("%s,%s,%s"), strZup, strZdn, strRes);
}


void getJumpSpotTypeV08(int nP1, int nC1, int nP2, int nC2, FlyingParam& param)
{
	if (PIERCING_SHOTMARKING == nP1 || CUTTING_SHOTMARKING == nC1)
	{
		param.jump.spotFrom = spotShotMarking;
		param.zJumpStart = param.zShotMarkingGap;
	}
	else
	{
		switch (nC1)
		{
		case CUTTING_ENGRAVEMARKING:
			param.jump.spotFrom = spotMarking;
			param.zJumpStart = param.zMarkingGap;
			break;
		default:
			param.jump.spotFrom = spotCutting;
			param.zJumpStart = (0 < param.zCuttingHMISet)? param.zCuttingHMISet: param.zCuttingGapCW;
			break;
		}
	}

	if (PIERCING_SHOTMARKING == nP2 || CUTTING_SHOTMARKING == nC2)
	{
		param.jump.spotTo = spotShotMarking;
		param.zJumpTarget = param.zShotMarkingGap;
	}
	else
	{
		switch (nC2)
		{
		case CUTTING_ENGRAVEMARKING:
			param.jump.spotTo = spotMarking;
			param.zJumpTarget = param.zMarkingGap;
			break;
		default:
			param.jump.spotTo = spotPiercing;
			param.zJumpTarget = param.zPiercingGap;
			break;
		}
	}
	return;
}

void getJumpSpotTypeV16(int nP1, int nC1, int nP2, int nC2, FlyingParam& param)
{
	if (PIERCING_V16_SHOTMARKING == nP1)
	{
		param.jump.spotFrom = spotShotMarking;
		param.zJumpStart = param.zShotMarkingGap;
	}
	else
	{
		switch (nC1)
		{
		case CUTTING_V16_MARKING:
			param.jump.spotFrom = spotMarking;
			param.zJumpStart = param.zMarkingGap;
			break;
		default:
			param.jump.spotFrom = spotCutting;
			if (0 < param.zCuttingHMISet)
				param.zJumpStart = param.zCuttingHMISet;
			else
				param.zJumpStart = (CUTTING_V16_PULSE == nC1)? param.zCuttingGapPulse: param.zCuttingGapCW;
			break;
		}
	}
	if (PIERCING_V16_SHOTMARKING == nP2)
	{
		param.jump.spotTo = spotShotMarking;
		param.zJumpTarget = param.zShotMarkingGap;
	}
	else
	{
		switch (nC2)
		{
		case CUTTING_V16_MARKING:
			param.jump.spotTo = spotMarking;
			param.zJumpTarget = param.zMarkingGap;
			break;
		default:
			param.jump.spotTo = spotPiercing;
			param.zJumpTarget = param.zPiercingGap;
			break;
		}
	}
	return;
}

bool getHKCutString(CString& strHKCUT, CAM_CONTOUR& c)
{
	if (3 > c.numCodes)
	{
		ASSERT(10 == c.nCutting);
		return true;
	}

	int iCut = strHKCUT.Find(_T("HKCUT"));
	if (0 > iCut)
	{
		ASSERT(FALSE);
		return false;
	}
	CAM_CODE* pElem = c.pCode;
	Point2d pt1(pElem[1].X-pElem[0].X, pElem[1].Y-pElem[0].Y);
	Point2d pt2(pElem[2].X-pElem[1].X, pElem[2].Y-pElem[1].Y);
	if (G_Line == pElem[2].code || IsZero(pt1.AngleBetween(pt2)))
		return true;

	iCut = strHKCUT.Find(_T(','));
	if (0 > iCut)
	{
		ASSERT(FALSE);
		return false;
	}
	CString strNew = strHKCUT.Left(iCut+1);
	strHKCUT = strHKCUT.Mid(iCut+1);
	iCut = strHKCUT.Find(_T(','));
	if (0 > iCut)
	{
		ASSERT(FALSE);
		return false;
	}
	CString strTale = strHKCUT.Mid(iCut);

	strHKCUT = strNew + _T('1') + strTale;
	return true;
}

bool writeThroughHKCUT(FileMPF& loader, CAM_CONTOUR& c, FileWriter& file, CString& strErr)
{
	CString str;

	// get the 'HKPIE()' block
	do
	{
		if (!loader.feed(str))
		{
			strErr = _T("Block string mismatch for HKPIE()");
			return false;
		}
		file.WriteString(str+_T('\n'));

		if (0 <= str.Find(_T("HKPIE")))
		{
			break;
		}
	}
	while (1);

	// get the 'HKLEA()' block
	do
	{
		if (!loader.feed(str))
		{
			strErr = _T("Block string mismatch for HKLEA()");
			return false;
		}
		file.WriteString(str+_T('\n'));

		if (0 <= str.Find(_T("HKLEA")))
		{
			break;
		}
	}
	while (1);

	// get HKCUT() block
	do
	{
		if (!loader.feed(str))
		{
			strErr = _T("Block string mismatch for HKCUT()");
			return false;
		}
		if (0 <= str.Find(_T("HKCUT")))
		{
			if (getHKCutString(str, c))
			{
				file.WriteString(str+_T('\n'));
				break;
			}
			// else errors in some of the HKCUT() arguments
			strErr = _T("HKCUT() argument format error");
			return false;
		}
	}
	while (1);

	return true;
}

bool writeHKSCRCBlocks(FileMPF& loader, FileWriter& file, CString& strErr)
{
	CString str;
	while (loader.feed(str))
	{
		file.WriteString(str+_T('\n'));
		if (0 <= str.Find(_T("HKSTO")))
			return true;
	}
	strErr = _T("Block string mismatch for HKSCRC() - No HKSTO()");
	return false;
}

bool writeTraceBuf(const FlyingTrace& buf, FileWriter& file, const CString& strBlock)
{
	CString str, sBlock(strBlock);
	for (int i = 0; i < strBlock.GetLength(); ++i)
	{
		if (_T(',') == sBlock[i])
			sBlock.SetAt(i, _T(' '));
	}

	const Trace& h = buf.pXY[0];
	const Trace& v = buf.pZ[0];
	ASSERT(h.time_ms == v.time_ms);
	str.Format(_T("%s, %u, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f,\n"),
			   sBlock, h.time_ms, h.jerk, h.accel, h.feed, h.distance,
			   v.jerk, v.accel, v.feed, v.distance);
	file.WriteString(str);

	for (size_t i = 1; i < buf.trace_count; ++i)
	{
		const Trace& H = buf.pXY[i];
		const Trace& V = buf.pZ[i];
		ASSERT(H.time_ms == V.time_ms);
		str.Format(_T(" , %u, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f,\n"),
				   H.time_ms, H.jerk, H.accel, H.feed, H.distance,
				   V.jerk, V.accel, V.feed, V.distance);
		file.WriteString(str);
	}
	return true;
}

bool isGcodeBlock(LPCTSTR szBlock)
{
	CString strBlock(szBlock);
	int iPos = -1, nAddr = -1;
	CString G_ = strBlock.Tokenize(_T(" \t"), iPos);
	if (G_.IsEmpty() || _T('G') != G_[0]
		|| !GetNumber(G_, 1, nAddr) || 0 > nAddr || 3 < nAddr)
		return false;

	bool doContinue = true;
	bool isX = false, isY = false, isI = false, isJ = false, isF = false;
	do
	{
		CString str = strBlock.Tokenize(_T(" \t"), iPos);
		switch (str[0])
		{
		case _T('X'):
			if (isX || !IsNumeric(str))
				return false;
			isX = true;
			break;
		case _T('Y'):
			if (isY || !IsNumeric(str))
				return false;
			isY = true;
			break;
		case _T('I'):
			if (isI || !IsNumeric(str))
				return false;
			isI = true;
			break;
		case _T('J'):
			if (isJ || !IsNumeric(str))
				return false;
			isJ = true;
			break;
		case _T('F'):
			if (isF)	// || !IsNumeric(str, F, 1))	// Fly-cut block strings have a type of 'F=R-variable' arguments.
				return false;
			isF = true;
			break;
		default:
			doContinue = false;
			break;
		}
	}
	while (doContinue);

	return (isX || isY);
}

void initFlyingParamMetrics(const JumpParam& cfg, FlyingParam& param)
{
	if (MEASURES_INCHES == cfg.measureSystem)
	{
		param.zCuttingGapCW	= inchTomm(cfg.zCuttingGapNormal);
		param.zCuttingGapPulse = inchTomm(cfg.zCuttingGapPulse);
		param.zCuttingHMISet = cfg.useMPFSettings? 0: inchTomm(cfg.zCuttingGapHMISetting);
		param.zJumpHeight	= inchTomm(cfg.zJumpHeight);
		param.zPiercingGap	= inchTomm(cfg.zPiercingGap);
		param.zMarkingGap	= inchTomm(cfg.zMarkingGap);
		param.zShotMarkingGap = inchTomm(cfg.zShotMarkingGap);
		param.minJumpRadius	= inchTomm(cfg.minJumpRadius);
		param.maxJumpRadius	= inchTomm(cfg.maxJumpRadius);

		param.motorX.jerk = inchTomm(cfg.X.jerk);
		param.motorX.accl = inchTomm(cfg.X.accel);
		param.motorX.feed = inchTomm(cfg.X.speed)/60.0;

		param.motorY.jerk = inchTomm(cfg.Y.jerk);
		param.motorY.accl = inchTomm(cfg.Y.accel);
		param.motorY.feed = inchTomm(cfg.Y.speed)/60.0;

		param.motorZ.jerk = inchTomm(cfg.Z.jerk);
		param.motorZ.accl = inchTomm(cfg.Z.accel);
		param.motorZ.feed = inchTomm(cfg.Z.speed)/60.0;
	}
	else
	{
		param.zCuttingGapCW = cfg.zCuttingGapNormal;
		param.zCuttingGapPulse = cfg.zCuttingGapPulse;
		param.zCuttingHMISet = cfg.useMPFSettings? 0: cfg.zCuttingGapHMISetting;
		param.zJumpHeight = cfg.zJumpHeight;
		param.zPiercingGap = cfg.zPiercingGap;
		param.zMarkingGap = cfg.zMarkingGap;
		param.zShotMarkingGap = cfg.zShotMarkingGap;
		param.minJumpRadius = cfg.minJumpRadius;
		param.maxJumpRadius = cfg.maxJumpRadius;

		param.motorX.jerk = cfg.X.jerk*1000.0;
		param.motorX.accl = cfg.X.accel*1000.0;
		param.motorX.feed = cfg.X.speed/60.0;

		param.motorY.jerk = cfg.Y.jerk*1000.0;
		param.motorY.accl = cfg.Y.accel*1000.0;
		param.motorY.feed = cfg.Y.speed/60.0;

		param.motorZ.jerk = cfg.Z.jerk*1000.0;
		param.motorZ.accl = cfg.Z.accel*1000.0;
		param.motorZ.feed = cfg.Z.speed/60.0;
	}

	applyMotionGainFactor(cfg.X, param.motorX);
	applyMotionGainFactor(cfg.Y, param.motorY);
	applyMotionGainFactor(cfg.Z, param.motorZ);

	param.jumpSyncDelay = (0 < cfg.jumpSyncDelay)? cfg.jumpSyncDelay: 0;

	return;
}

int generateFlyingMode(FlyingParam& param, FlyingProfile& jump, bool isInches, const Point2d& pt1, const Point2d& pt2, double& h1, double& h2, double& r)
{
	Point2d d = pt2 - pt1;
	param.jump.xyAngleRad = atan2(d.y, d.x);
	param.jump.xyDistance = isInches? inchTomm(d.Magnitude()): d.Magnitude();

	//
	// Run flying mode optimization
	//
	int nRet = jump.generate(param);
	if (PROFILE_SUCCESS != nRet)
	{
		TRACE1("*** ERROR: can't generate jump profile (error = %d).\n", nRet);
		if (isInches)
		{
			h1 = mmToInch(param.zJumpHeight - param.zJumpStart);
			h2 = mmToInch(param.zJumpHeight - param.zJumpTarget);
		}
		else
		{
			h1 = param.zJumpHeight - param.zJumpStart;
			h2 = param.zJumpHeight - param.zJumpTarget;
		}
		r = 0;
		return nRet;
	}

	//
	// Flying mode profile generation done
	//
	nRet = jump.jumpInfo(h1, r, h2);
	ASSERT(IsSame(h1+param.zJumpStart, h2+param.zJumpTarget) && nJUMPTYPE_FLYING == nRet);
	if (isInches)
	{
		h1 = mmToInch(h1);
		h2 = mmToInch(h2);
		r = mmToInch(r);
	}

	return PROFILE_SUCCESS;
}

bool restartBuildMarkupText(fileVersion ver, RESTART& info)
{
	TCHAR sz[128] ={ 0 };

	// Restart header head
	_stprintf_s(sz, _countof(sz), _T(";%s\n"), csz_RestartHead);
	info.strMarkUp = sz;

	// Stop position information at the original MPF file
	_stprintf_s(sz, _countof(sz), _T(";<%s %s=%d %s=%d %s=%d %s=%d %s=%d /%s>\n"),
				csz_TagOriginalPart,
				csz_ElemOrgPartNo, info.partNo,
				csz_ElemOrgContNo, info.contourNo,
				csz_ElemOrgCodeIndex, info.iBlockCode,
				csz_ElemBlockNo, info.orgShapeBlockNo,
				csz_ElemReverse, info.isReverseOrder? 1: 0,
				csz_TagOriginalPart);
	info.strMarkUp += sz;

	// Restart information for the modified MPF file
	_stprintf_s(sz, _countof(sz), _T(";<%s %s=%d %s=%d %s=%d %s=%d %s=%.3f %s=%.3f /%s>\n"),
				csz_TagModifiedPart,
				csz_ElemBlockNo, info.modShapeBlockNo,
				csz_ElemLineNo, info.restartShapeLineNo,
				csz_ModContourLineNo, info.restartContourLineNo,
				csz_ElemLineNoOffset, info.orgModLineOffset,
				csz_RestartWCSx, info.wcsX,
				csz_RestartWCSy, info.wcsY,
				csz_TagModifiedPart);
	info.strMarkUp += sz;

	// [Part start] enabling flag and the restart part/contour number
	switch (ver)
	{
	case verV08:
		_stprintf_s(sz, _countof(sz),
			_T("R521=2 ")			// Enable search part,contour: 1=off, 2=contour, 3=part
			_T("R519=%d R520=%d\n"),// Specify restart part & contour number
			info.partNo, info.contourNo);
		break;
	case verV16:
		_stprintf_s(sz, _countof(sz),
			_T("R90=%d R91=%d\n"),	// Specify restart part & contour number
			info.partNo, info.contourNo);
		break;
	case verV16A05:
		_stprintf_s(sz, _countof(sz),
			_T("R772=2 ")			// Enable search part,contour: 1=off, 2=on
			_T("R768=%d R769=%d\n"),// Specify restart part & contour number
			info.partNo, info.contourNo);
		break;
	default:
		ASSERT(FALSE);
		info.strMarkUp = _T("Restart setting is not specified for this version of MPF\n");
		return false;
		break;
	}
	info.strMarkUp += sz;

	// Restart header tail
	_stprintf_s(sz, _countof(sz), _T(";%s\n\n"), csz_RestartTail);
	info.strMarkUp += sz;

	return true;
}


bool restartMakeHKOST(CString& strHKOST, int nNumContours, RESTART& info)
{
	int iHKOST = strHKOST.Find(_T("HKOST"));
	if (0 > iHKOST)
	{
		info.strMarkUp = _T("HKOST not found");
		return false;
	}
	int iComma = iHKOST + 5;
	for (int i = 0; i < 3; ++i)	// x, y origin and rotation
	{
		iComma = strHKOST.Find(_T(','), iComma + 1);
		if (0 > iComma)
		{
			info.strMarkUp = _T("HKOST format error");
			return false;
		}
	}
	// NXXXX HKOST(partOrgX, partOrgY, partRotation, <- now at this position

	int iRest = strHKOST.Find(_T(','), iComma + 1);	// the original block number of the part shape
	if (0 < iRest)
		iRest = strHKOST.Find(_T(','), iRest + 1);	// the original number of count in the part

	info.strHKOST.Format(_T("%s%d,%d%s\n"),
					   strHKOST.Left(iComma+1),
					   info.modShapeBlockNo, nNumContours,
					   (0 < iRest)? strHKOST.Mid(iRest): _T(")"));
	return true;
}

bool replaceHKOSTBlockNo(CString& strHKOST, int nOldBlockNo, int nNewBlockNo)
{
	CAMViewerFactory::appendRestartLog(
		_T("replaceHKOSTBlockNo(\n")
		_T("                    strHKOST    = %s\n")
		_T("                    nOldBlockNo = %d\n")
		_T("                    nNewBlockNo = %d)\n"),
		strHKOST, nOldBlockNo, nNewBlockNo);

	if (nOldBlockNo == nNewBlockNo)
	{
		CAMViewerFactory::appendRestartLog(_T(" no need to replace: the same numbers\n"));
		return true;
	}

	int iHKOST = strHKOST.Find(_T("HKOST"));
	if (0 > iHKOST)
	{
		CAMViewerFactory::appendRestartLog(_T("** Invalid strHKOST() = can\'t find \'HKOST\'\n"));
		ASSERT(FALSE);
		return false;
	}

	int iComma = iHKOST + 5;
	for (int i = 0; i < 3; ++i)	// x, y origin and rotation
	{
		iComma = strHKOST.Find(_T(','), iComma + 1);
		if (0 > iComma)
		{
			CAMViewerFactory::appendRestartLog(_T("** HKOST format error: error in proceeding x, y, rotation palce holding commas\n"));
			return false;
		}
	}
	// NXXXX HKOST(partOrgX, partOrgY, partRotation, <- now at this position

	int iRest = strHKOST.Find(_T(','), iComma + 1);	// the original block number of the part shape
	if (0 < iRest)
	{
		CString strBlockNo = strHKOST.Mid(iComma + 1, iRest - iComma - 1);
		const int N = _tstoi(strBlockNo);
		if (N != nOldBlockNo)
		{
			CAMViewerFactory::appendRestartLog(_T("** Block number mismatch: HKOST(%d) vs. Given(%d)\n"), N, nOldBlockNo);
			return false;
		}
		strHKOST.Format(_T("%s%d%s"), strHKOST.Left(iComma+1), nNewBlockNo, strHKOST.Mid(iRest));
	}
	else
	{
		CString strBlockNo = strHKOST.Mid(iComma + 1);
		const int N = _tstoi(strBlockNo);
		if (N != nOldBlockNo)
		{
			CAMViewerFactory::appendRestartLog(_T("** Block number mismatch: HKOST(%d) vs. Given(%d)\n"), N, nOldBlockNo);
			return false;
		}
		strHKOST.Format(_T("%s%d%s"), strHKOST.Left(iComma+1), nNewBlockNo, _T(")"));
	}

	return true;
}

bool restartMakeHKSTR(CString& strHKSTR, int nPiercing, double start_x, double start_y, RESTART& info)
{
	int iCut = strHKSTR.Find(_T("HKSTR"));	// HKSTR
	if (0 > iCut)
	{
		ASSERT(FALSE);
		return false;
	}
	iCut = strHKSTR.Find(_T(','), iCut+1);	// piercing
	if (0 > iCut)
	{
		ASSERT(FALSE);
		return false;
	}

	CString str = strHKSTR.Mid(iCut+1);
	iCut = str.Find(_T(','));			// cutting
	if (0 > iCut)
	{
		ASSERT(FALSE);
		return false;
	}
	int nCutting = _tstoi(str.Left(iCut));
	str = str.Mid(iCut+1);
	iCut = str.Find(_T(','));			// x
	if (0 > iCut)
	{
		ASSERT(FALSE);
		return false;
	}
	iCut = str.Find(_T(','), iCut+1);	// y
	if (0 > iCut)
	{
		ASSERT(FALSE);
		return false;
	}
	str = str.Mid(iCut+1);

	int nNewBlockNumber = info.modShapeBlockNo + info.contourNo - 1;
	nPiercing = (restartPiercing == info.pos)? nPiercing: 0;
	info.strHKSTR.Format(_T("N%d HKSTR(%d,%d,%.4f,%.4f,%s\n"),	// new block number with no-piercing
					nNewBlockNumber, nPiercing, nCutting, start_x, start_y, str);
	return true;
}

void makeRestartAtNesting(const int nPartNo, const CAM_PART& part, const CAM_CONTOUR& contour, RESTART& info, fileVersion ver, CString& strLog)
{
	// Restart position specifier
	info.pos = restartPart;

	// Restart part number
	if (nPartNo != info.partNo)
	{
		appendRestartLog(strLog, _T("* Warning, restart part No(%d) mismatch with nesting part no(%d)\n"), info.partNo, nPartNo);
		appendRestartLog(strLog, _T("  Restart part No is adjusted to %d\n"), nPartNo);
		info.partNo = nPartNo;
	}

	// Restart contour number
	info.contourNo = 1;

	// Restart block number
	info.orgShapeBlockNo = info.modShapeBlockNo = 10000*(part.iCAMShape + 1) + 1;

	// Restart position in WCS
	info.wcsX = part.origin_X + contour.x_start;
	info.wcsY = part.origin_Y + contour.y_start;

	// Make restart mark-up text
	restartBuildMarkupText(ver, info);

	return;
}


//////////////////////////////////////////////////////////////////////////
// CAMViewerFactory class implementation
//

bool CAMViewerFactory::isRestartLogEnabled()
{
	return (FALSE == s_logFolderRestart.IsEmpty());
}

bool CAMViewerFactory::setRestartLogFolder(LPCTSTR pszFolder)
{
	if (nullptr == pszFolder)
	{
		writeRestartLog(_T("Restart logging disabled.\n"));
		s_logFolderRestart.Empty();
		return true;
	}

	DWORD attrib = ::GetFileAttributes(pszFolder);
	if (INVALID_FILE_ATTRIBUTES == attrib
		|| FILE_ATTRIBUTE_DIRECTORY != (FILE_ATTRIBUTE_DIRECTORY&attrib))
	{
		writeRestartLog(_T("Restart log folder setting error:\n            invalid folder [%s]"), pszFolder);
		ASSERT(FALSE);
		return false;
	}

	s_logFolderRestart = pszFolder;
	if (_T('\\') == s_logFolderRestart.GetAt(s_logFolderRestart.GetLength()-1))
		s_logFolderRestart.ReleaseBuffer(s_logFolderRestart.GetLength()-1);

	writeRestartLog(_T("Restart logging enabled."));

	return true;
}

void CAMViewerFactory::writeRestartLog(LPCTSTR pszFormat, ...)
{
	if (!isRestartLogEnabled())
		return;

	// construct log message
	va_list args;
	va_start(args, pszFormat);
	_vstprintf_s(s_szLogBuf, _countof(s_szLogBuf), pszFormat, args);

	// write log message to the log file
	CTime t = CTime::GetCurrentTime();
	TCHAR szFilePath[MAX_PATH];
	_stprintf_s(szFilePath, MAX_PATH, _T("%s\\%s_Restart.log"), s_logFolderRestart, t.Format(_T("%Y-%m-%d")));
	try
	{	// CStdioFile -> FILE* & CRT fopen, fprintf ·Î ¹Ù²Ü°Í!     
		CStdioFile file(szFilePath, CFile::modeCreate | CFile::modeNoTruncate | CFile::modeWrite | CFile::typeText);
		file.SeekToEnd();

		CString strLog;
		strLog.Format(_T("[%s], %s\n"), t.Format(_T("%H:%M:%S")), s_szLogBuf);
		file.WriteString(strLog);
	}
	catch (CFileException* pe)
	{
		CString strErr;
		strErr.Format(_T("File could not be opened, cause = %d\n"), pe->m_cause);
		pe->Delete();
		AfxMessageBox(strErr);
	}

	return;
}

void CAMViewerFactory::appendRestartLog(LPCTSTR pszFormat, ...)
{
	if (!isRestartLogEnabled())
		return;

	// construct log message
	va_list args;
	va_start(args, pszFormat);
	_vstprintf_s(s_szLogBuf, _countof(s_szLogBuf), pszFormat, args);

	// append log message to the log file
	CTime t = CTime::GetCurrentTime();
	TCHAR szFilePath[MAX_PATH];
	_stprintf_s(szFilePath, MAX_PATH, _T("%s\\%s_Restart.log"), s_logFolderRestart, t.Format(_T("%Y-%m-%d")));
	try
	{
		CStdioFile file(szFilePath, CFile::modeCreate | CFile::modeNoTruncate | CFile::modeWrite | CFile::typeText);
		file.SeekToEnd();

		CString strLog;
		strLog.Format(_T("          , %s\n"), s_szLogBuf);
		file.WriteString(strLog);
	}
	catch (CFileException* pe)
	{
		CString strErr;
		strErr.Format(_T("File could not be opened, cause = %d\n"), pe->m_cause);
		pe->Delete();
		AfxMessageBox(strErr);
	}

	return;
}


//////////////////////////////////////////////////////////////////////////
// class CAMViewerFactory

CAMViewerFactory::~CAMViewerFactory()
{
	POSITION pos = m_listContainer.GetHeadPosition();
	while (pos)
	{
		CAMContainer* pContainer = (CAMContainer*)m_listContainer.GetNext(pos);
		if (AfxIsValidAddress(pContainer, sizeof(CAMContainer)) && ::IsWindow(pContainer->viewer.m_hWnd))
			pContainer->viewer.DestroyWindow();
		delete pContainer;
	}
	m_listContainer.RemoveAll();
}


HWND CAMViewerFactory::CreateViewer(HWND hWndParent, UINT ID, int x, int y, int cx, int cy, TraceMouseCoords pfn)
{
	CWnd* pParent = CWnd::FromHandle(hWndParent);
	if (!pParent)
		return NULL;

	CAMContainer* pContainer = new CAMContainer;
	if (!pContainer)
		return NULL;

	CRect rect(x, y, x+cx, y+cy);
	if (!pContainer->viewer.Create(rect, pParent, ID))
	{
		delete pContainer;
		return NULL;
	}

	if (pfn)
		pContainer->viewer.SetCallbackForTraceMouseCoords(pfn);

	m_listContainer.AddTail(pContainer);

	return pContainer->viewer.m_hWnd;
}


int CAMViewerFactory::DestroyViewer(HWND hWnd)
{
	POSITION pos = NULL;
	CAMContainer* pContainer = FindContainer(hWnd, &pos);
	if (NULL == pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.DestroyWindow();
	delete pContainer;
	m_listContainer.RemoveAt(pos);

	return CV_NOERROR;
}


int CAMViewerFactory::Load(HWND hWnd, LPCTSTR pszFile, CString& strError)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	InterfaceMPF mpf;
	if (!mpf.Load(pszFile, *pContainer))
	{
		strError = mpf.GetLastErrorMessage();
		return CVERR_FILELOAD;
	}

	pContainer->ArrangeRestartFile();

	if (!pContainer->viewer.UpdateCAMData(pContainer->camData))
		return CVERR_CAMDATA;

	if (pContainer->loader.isRestartFile())
	{
		const RESTART_LOG& resLog = pContainer->loader.getRestartInfo();
		int nPart = 1, nContour = 1, iElement = 0;
		bool isReverse = (TRUE == resLog.isReverse);
		if (isReverse)
			nPart = pContainer->camData.num_Parts;

		pContainer->viewer.StartCuttingProgress(nPart, nContour, isReverse);
		nPart = resLog.partNo;
		nContour = resLog.contourNo;
		pContainer->viewer.UpdateCuttingProgress(nPart, nContour, NULL, 0, resLog.wcsX, resLog.wcsY, false);
	}
	pContainer->viewer.SetFocus();

	return CV_NOERROR;
}

int CAMViewerFactory::DeleteContents(HWND hWnd)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.DeleteContents();
	return CV_NOERROR;
}

int CAMViewerFactory::RelocateView(HWND hWnd, int x, int y, int cx, int cy)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	if (::IsWindow(pContainer->viewer.m_hWnd))
	{
		pContainer->viewer.SetWindowPos(0, x, y, cx, cy, SWP_NOZORDER);
		return CV_NOERROR;
	}
	return CVERR_WINDOW_NOT_READY;
}


int CAMViewerFactory::Zoom(HWND hWnd, enZoom how)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	switch (how)
	{
	case enActualSize:
		pContainer->viewer.ActualSize();
		break;

	case enZoomIn: case enZoomOut:
		pContainer->viewer.Zoom(enZoomIn == how);
		break;

	case enFullView:
		pContainer->viewer.FitDrawingToWindow();
		break;

	default:
		return CVERR_INVALID_OPT_VALUE;
	}
	pContainer->viewer.SetFocus();
	return CV_NOERROR;
}


int CAMViewerFactory::SetOption(HWND hWnd, int iOption, unsigned int nValue)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	switch (iOption)
	{
	case iOPT_DRAW_PIERCING:
		pContainer->viewer.DrawPiercingSpot((0 != nValue));
		break;
	case iOPT_PART_NUMBER:
		pContainer->viewer.DisplayPartNo((0 != nValue));
		break;
	case iOPT_CONTOUR_NUMBER:
		pContainer->viewer.DisplayContourNo((0 != nValue));
		break;
	case iOPT_TRACE_CUTTINGSPOT:
		pContainer->viewer.EnsureCuttingSpotVisible((0 != nValue));
		break;
	case iOPT_PARTNO_HEIGHT:
		if (0 >= int(nValue))
			return CVERR_INVALID_OPT_VALUE;
		pContainer->viewer.SetPartNoHeight(int(nValue));
		break;
	case iOPT_CONTOURNO_HEIGHT:
		if (0 >= int(nValue))
			return CVERR_INVALID_OPT_VALUE;
		pContainer->viewer.SetContourNoHeight(int(nValue));
		break;
	case iOPT_LIMIT_PART_CONT_NO:
		pContainer->viewer.LimitPartContourNumbers(nValue);
		break;
	case iOPT_COLOR_BACKGROUND:
		pContainer->viewer.UpdateColor(enColorBackground, COLORREF(nValue));
		break;
	case iOPT_COLOR_WORKPIECE:
		pContainer->viewer.UpdateColor(enColorCanvas, COLORREF(nValue));
		break;
	case iOPT_COLOR_PART_NUMBER:
		pContainer->viewer.UpdateColor(enColorPartNo, COLORREF(nValue));
		break;
	case iOPT_COLOR_CONTOUR_NUMBER:
		pContainer->viewer.UpdateColor(enColorContourNo, COLORREF(nValue));
		break;
	case iOPT_COLOR_PIERCING:
		pContainer->viewer.UpdateColor(enColorPiercing, COLORREF(nValue));
		break;
	case iOPT_COLOR_LEADIN:
		pContainer->viewer.UpdateColor(enColorLeadIn, COLORREF(nValue));
		break;
	case iOPT_COLOR_CONTOUR:
		pContainer->viewer.UpdateColor(enColorContour, COLORREF(nValue));
		break;
	case iOPT_COLOR_MARKING:
		pContainer->viewer.UpdateColor(enColorEngraving, COLORREF(nValue));
		break;
	case iOPT_COLOR_CUTTING:
		pContainer->viewer.UpdateColor(enColorCutProgress, COLORREF(nValue));
		break;
	default:
		return CVERR_INVALID_OPTION;
		break;
	}
	pContainer->viewer.SetFocus();
	return CV_NOERROR;
}


int CAMViewerFactory::GetOption(HWND hWnd, int iOption, unsigned int& nValue)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	switch (iOption)
	{
	case iOPT_DRAW_PIERCING:
		nValue = pContainer->viewer.IsPiercingDisplayed()? 1: 0;
		break;
	case iOPT_PART_NUMBER:
		nValue = pContainer->viewer.IsPartNoEnabled()? 1: 0;
		break;
	case iOPT_CONTOUR_NUMBER:
		nValue = pContainer->viewer.IsContourNoEnabled()? 1: 0;
		break;
	case iOPT_TRACE_CUTTINGSPOT:
		nValue = pContainer->viewer.IsCuttingSpotTracking()? 1: 0;
		break;
	case iOPT_PARTNO_HEIGHT:
		if (0 >= int(nValue))
			return CVERR_INVALID_OPT_VALUE;
		nValue = pContainer->viewer.GetPartNoHeight();
		break;
	case iOPT_CONTOURNO_HEIGHT:
		if (0 >= int(nValue))
			return CVERR_INVALID_OPT_VALUE;
		nValue = pContainer->viewer.GetContourNoHeight();
	case iOPT_COLOR_BACKGROUND:
		nValue = pContainer->viewer.GetColor(enColorBackground);
		break;
	case iOPT_COLOR_WORKPIECE:
		nValue = pContainer->viewer.GetColor(enColorCanvas);
		break;
	case iOPT_COLOR_PART_NUMBER:
		nValue = pContainer->viewer.GetColor(enColorPartNo);
		break;
	case iOPT_COLOR_CONTOUR_NUMBER:
		nValue = pContainer->viewer.GetColor(enColorContourNo);
		break;
	case iOPT_COLOR_PIERCING:
		nValue = pContainer->viewer.GetColor(enColorPiercing);
		break;
	case iOPT_COLOR_LEADIN:
		nValue = pContainer->viewer.GetColor(enColorLeadIn);
		break;
	case iOPT_COLOR_CONTOUR:
		nValue = pContainer->viewer.GetColor(enColorContour);
		break;
	case iOPT_COLOR_MARKING:
		nValue = pContainer->viewer.GetColor(enColorEngraving);
		break;
	case iOPT_COLOR_CUTTING:
		nValue = pContainer->viewer.GetColor(enColorCutProgress);
		break;
	default:
		return CVERR_INVALID_OPTION;
		break;
	}
	return CV_NOERROR;
}

int CAMViewerFactory::GetOptions(HWND hWnd, DrawingOption& opt)
{
	CAMContainer* pContainer = NULL;
	if (NULL == hWnd || NULL == (pContainer=FindContainer(hWnd)))
	{
		CCAMViewWnd::GetDrawingOptions(opt);
	}
	else
	{
		pContainer->viewer.GetCurrDrawingOptions(opt);
	}
	return CV_NOERROR;
}

double CAMViewerFactory::GetZoomScale(HWND hWnd)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return 0;
	return pContainer->viewer.GetZoomScale();
}

int CAMViewerFactory::GetWholeSize(HWND hWnd, double& width, double& height)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	if (pContainer->viewer.HasContents())
	{
		pContainer->viewer.GetWholeSize(width, height);
		return CV_NOERROR;
	}
	return CVERR_NOCONTENTS;
}

inline void StringCopy(PTSTR pszDst, const int count_dstBuf, const CString& str)
{
	if (str.IsEmpty())
	{
		pszDst[0] = 0;
	}
	else if (str.GetLength() < count_dstBuf)
	{
		_tcscpy(pszDst, str);
	}
	else
	{
		_tcscpy(pszDst, str.Left(count_dstBuf-1));
	}
}

int CAMViewerFactory::GetFileInfo(HWND hWnd, CAMFILEINFO* pInfo)
{
	if (!AfxIsValidAddress(pInfo, sizeof(CAMFILEINFO)))
		return CVERR_INVALID_ARGS;

	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	const CAM_DATA& camInfo = pContainer->camData;
	if (camInfo.HasContents())
	{
		const camLayout& layout = pContainer->viewer.GetCAMLayoutInfo();
		if (layout._num_parts != camInfo.num_Parts)
			return CVERR_CAMDATA;

		CString sVersion;
		camInfo.GetVersion(sVersion);
		if (sVersion.GetLength() > c_camMiscBufLen)
		{
			_tcsncpy(pInfo->sVersion, sVersion, c_camMiscBufLen-1);
			pInfo->sVersion[c_camMiscBufLen-1] = 0;
		}
		else
		{
			_tcscpy(pInfo->sVersion, sVersion);
		}

		if (camInfo.strFileName.IsEmpty())
		{
			pInfo->sName[0] = 0;
			pInfo->sPath[0] = 0;
		}
		else
		{
			int iCut = camInfo.strFileName.ReverseFind(_T('\\'));
			if (0 <= iCut)
			{
				CString strFile = camInfo.strFileName.Mid(iCut+1);
				StringCopy(pInfo->sName, c_camFilenameBufLen, strFile);
				StringCopy(pInfo->sPath, c_camFilenameBufLen, camInfo.strFileName);
			}
			else
			{
				StringCopy(pInfo->sName, c_camFilenameBufLen, camInfo.strFileName);
				_tcscpy(pInfo->sPath, pInfo->sName);
			}
		}
		StringCopy(pInfo->sCDBname, c_camDBnameBufLen, camInfo.strDBName);
		StringCopy(pInfo->sMaterial, c_camMiscBufLen, camInfo.strMaterial);
		StringCopy(pInfo->sAssistGas, c_camMiscBufLen, camInfo.strAssistGas);

		pInfo->thickness = camInfo.nMaterialThickness;	// workpiece thickness in millimeters
		pInfo->width = camInfo.sheetWidth;
		pInfo->height = camInfo.sheetHeight;	// dimension of the workpiece in millimeters

		pInfo->number_of_part_types = camInfo.num_Shapes;
		pInfo->number_of_parts = camInfo.num_Parts;	// the number of parts
		pInfo->number_of_contours_total = 0;	// the total number of contours
		pInfo->contour_length_total_mm = 0;

		for (int i = 0; i < camInfo.num_Parts; ++i)
		{
			if (!camInfo.pPart)
				return CVERR_CAMDATA;

			if (camInfo.pPart[i].iCAMShape < 0)
				return CVERR_CAMDATA;

			CAM_SHAPE& cPart = camInfo.pCAMShape[camInfo.pPart[i].iCAMShape];
			if (!cPart.IsValid())
				return CVERR_CAMDATA;

			pInfo->number_of_contours_total += cPart.numContours;

			const camPart& part = layout._part[i];
			if (part.num_contours != cPart.numContours)
				return CVERR_CAMDATA;

			for (int j = 0; j < part.num_contours; ++j)
			{
				pInfo->contour_length_total_mm += part.pContour[j].GetLength();
			}
		}
		return CV_NOERROR;
	}
	return CVERR_NOCONTENTS;
}


int CAMViewerFactory::WriteJumpOptimizedFile(HWND hWnd, LPCTSTR pszFile, LPVOID pParam, CString& strError)
{
	JumpParam* pData = (JumpParam*)pParam;
	if (!AfxIsValidAddress(pData, sizeof(JumpParam)))
	{
		strError = _T("Invalid address of Jump parameter buffer");
		return CVERR_INVALID_ARGS;
	}

	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
	{
		strError = _T("Can't find the CAM viewer container");
		return CVERR_WINDOW_NOTFOUND;
	}

	const CAM_DATA& camData = pContainer->camData;
	if (!camData.HasContents())
	{
		strError = _T("CAM data buffer is empty");
		return CVERR_NOCONTENTS;
	}

	FileWriter file;
	if (!file.Open(pszFile))
	{
		strError = _T("File open error.");
		return CVERR_FILECREATE;
	}

	return pContainer->WriteFlyingOptimization(*pData, file, strError);
}

int CAMViewerFactory::WriteRPPOptimizedFile(HWND hWnd, LPCTSTR pszFile, CString& strError)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
	{
		strError = _T("Can't find the CAM viewer container");
		return CVERR_WINDOW_NOTFOUND;
	}

	const CAM_DATA& camData = pContainer->camData;
	if (!camData.HasContents())
	{
		strError = _T("CAM data buffer is empty");
		return CVERR_NOCONTENTS;
	}

	FileWriter file;
	if (!file.Open(pszFile))
	{
		strError = _T("File open error.");
		return CVERR_FILECREATE;
	}

	return pContainer->WriteRPPOptimization(file, strError);
}

int CAMViewerFactory::WriteJumpRPPOptimizedFile(HWND hWnd, LPCTSTR pszFile, LPVOID pParam, CString& strError)
{
	JumpParam* pData = (JumpParam*)pParam;
	if (!AfxIsValidAddress(pData, sizeof(JumpParam)))
	{
		strError = _T("Invalid address of Jump parameter buffer");
		return CVERR_INVALID_ARGS;
	}

	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
	{
		strError = _T("Can't find the CAM viewer container");
		return CVERR_WINDOW_NOTFOUND;
	}

	const CAM_DATA& camData = pContainer->camData;
	if (!camData.HasContents())
	{
		strError = _T("CAM data buffer is empty");
		return CVERR_NOCONTENTS;
	}

	FileWriter file;
	if (!file.Open(pszFile))
	{
		strError = _T("File open error.");
		return CVERR_FILECREATE;
	}

	return pContainer->WriteFlyingRPPOptimization(*pData, file, strError);
}


int CAMViewerFactory::WriteJumpOptimizedFileWithOrgFile(LPCTSTR pszOrgFile, LPCTSTR pszDestFile, LPVOID pParam, CString& strError)
{
	try
	{
		JumpParam* pData = (JumpParam*)pParam;
		if (!AfxIsValidAddress(pData, sizeof(JumpParam)))
		{
			strError = _T("Invalid address of Jump parameter buffer");
			return CVERR_INVALID_ARGS;
		}

		CAMContainer container;
		InterfaceMPF mpf;
		if (!mpf.Load(pszOrgFile, container))
		{
			strError = mpf.GetLastErrorMessage();
			return CVERR_FILELOAD;
		}

		if (!container.camData.HasContents())
		{
			strError = _T("CAM data buffer is empty");
			return CVERR_NOCONTENTS;
		}

		FileWriter file;
		if (!file.Open(pszDestFile))
		{
			strError = _T("File open error.");
			return CVERR_FILECREATE;
		}
		return container.WriteFlyingOptimization(*pData, file, strError);
	}
	catch (CException* e)
	{
		TCHAR szErr[1024] ={ 0 };
		e->GetErrorMessage(szErr, sizeof(szErr));
		strError.Format(_T("Exception has been occurred while calling WriteJumpOptimizedFileWithOrgFile. [Description:%s]"), szErr);
		return CVERR_INVALID_ARGS;
	}
}

int CAMViewerFactory::WriteRPPOptimizedFileWithOrgFile(LPCTSTR pszOrgFile, LPCTSTR pszDestFile, CString& strError)
{
	CAMContainer container;
	InterfaceMPF mpf;
	if (!mpf.Load(pszOrgFile, container))
	{
		strError = mpf.GetLastErrorMessage();
		return CVERR_FILELOAD;
	}

	if (!container.camData.HasContents())
	{
		strError = _T("CAM data buffer is empty");
		return CVERR_NOCONTENTS;
	}

	FileWriter file;
	if (!file.Open(pszDestFile))
	{
		strError = _T("File open error.");
		return CVERR_FILECREATE;
	}

	return container.WriteRPPOptimization(file, strError);
}

int CAMViewerFactory::WriteJumpRPPOptimizedFileWithOrgFile(LPCTSTR pszOrgFile, LPCTSTR pszDestFile, LPVOID pParam, CString& strError)
{
	JumpParam* pData = (JumpParam*)pParam;
	if (!AfxIsValidAddress(pData, sizeof(JumpParam)))
	{
		strError = _T("Invalid address of Jump parameter buffer");
		return CVERR_INVALID_ARGS;
	}

	CAMContainer container;
	InterfaceMPF mpf;
	if (!mpf.Load(pszOrgFile, container))
	{
		strError = mpf.GetLastErrorMessage();
		return CVERR_FILELOAD;
	}

	if (!container.camData.HasContents())
	{
		strError = _T("CAM data buffer is empty");
		return CVERR_NOCONTENTS;
	}

	FileWriter file;
	if (!file.Open(pszDestFile))
	{
		strError = _T("File open error.");
		return CVERR_FILECREATE;
	}

	return container.WriteFlyingRPPOptimization(*pData, file, strError);
}


int CAMViewerFactory::WriteFlyingJumpTraceFile(HWND hWnd, LPCTSTR pszFile, LPVOID pParam, CString& strError)
{
	JumpParam* pData = (JumpParam*)pParam;
	if (!AfxIsValidAddress(pData, sizeof(JumpParam)))
	{
		strError = _T("Invalid address of Jump parameter buffer");
		return CVERR_INVALID_ARGS;
	}

	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
	{
		strError = _T("Can't find the CAM viewer container");
		return CVERR_WINDOW_NOTFOUND;
	}

	const CAM_DATA& camData = pContainer->camData;
	const UINT ms_sampling = 1;

	if (camData.HasContents())
	{
		FileWriter file;
		if (file.Open(pszFile))
		{
			if (pContainer->WriteFlyingJumpTrace(*pData, ms_sampling, file, strError))
				return CV_NOERROR;
			else
				return CVERR_CAMDATA;
		}
		else
		{
			strError = _T("Trace file creation error.");
			return CVERR_FILECREATE;
		}
	}
	return CVERR_NOCONTENTS;
}


int CAMViewerFactory::WriteRestartWorkFile(const RestartInfo& option, LPCTSTR pszSrcFile, LPCTSTR pszDstFile, CString& strError)
{
	CAMViewerFactory::writeRestartLog(_T("RestartWork MPF createion: %s -> %s"), pszSrcFile, pszDstFile);

	CAMContainer container;
	InterfaceMPF mpf;
	if (!mpf.Load(pszSrcFile, container))
	{
		strError = mpf.GetLastErrorMessage();
		CAMViewerFactory::writeRestartLog(_T("Source file load error: %s"), strError);
		return CVERR_FILELOAD;
	}
	if (!container.camData.HasContents())
	{
		strError = _T("CAM data buffer is empty");
		CAMViewerFactory::writeRestartLog(_T("Error: the source file is empty."));
		return CVERR_NOCONTENTS;
	}
	container.ArrangeRestartFile();

	FileWriter file;
	if (!file.Open(pszDstFile))
	{
		strError = _T("File open error.");
		CAMViewerFactory::writeRestartLog(_T("Error: can't create the target file [%s]."), pszDstFile);
		return CVERR_FILECREATE;
	}

	int nRC = container.WriteRestartWorkFile(option, file, strError);
	if (CV_NOERROR != nRC)
		::DeleteFile(pszDstFile);

	CAMViewerFactory::writeRestartLog(_T("RestartWork Finished: code = %d"), nRC);

	return nRC;
}


int CAMViewerFactory::StartCutting(HWND hWnd, int nPartFrom, int nContourFrom, bool IsReverse)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	if (!pContainer->viewer.HasContents())
		return CVERR_WINDOW_NOT_READY;

	if (!pContainer->viewer.StartCuttingProgress(nPartFrom, nContourFrom, IsReverse))
		return CVERR_INVALID_PART_NO;

	return CV_NOERROR;
}

int CAMViewerFactory::UpdateCutting(HWND hWnd, int part, int contour, const char* currentBlock, double progress, double xwcs, double ywcs, bool isGcodeBlock)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	if (pContainer->viewer.UpdateCuttingProgress(part, contour, currentBlock, progress, xwcs, ywcs, isGcodeBlock))
		return CV_NOERROR;

	return CVERR_INVALID_PART_NO;
}

int CAMViewerFactory::UpdateCutting(HWND hWnd, int part, int contour, int mpfLineNo, double progress, double xwcs, double ywcs)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (pContainer)
		return pContainer->UpdateCuttingProgress(part, contour, mpfLineNo, progress, xwcs, ywcs);
	return CVERR_WINDOW_NOTFOUND;
}

int CAMViewerFactory::GetContourLength(HWND hWnd, int part, int contour, double& length, bool isReset)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	return pContainer->viewer.GetContourLength(part, contour, length, isReset);
}

int CAMViewerFactory::GetPartCount(HWND hWnd, int& partCount)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;
	return pContainer->viewer.GetPartCount(partCount);
}

int CAMViewerFactory::GetContourCount(HWND hWnd, int part, int& contourCount)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	contourCount = -1;
	return pContainer->viewer.GetContourCount(part, contourCount);
}

int CAMViewerFactory::GetElementCount(HWND hWnd, int part, int contour, int& elementCount)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;
	return pContainer->viewer.GetElementCount(part, contour, elementCount);
}

int CAMViewerFactory::GetElementInfo(HWND hWnd, int part, int contour, int element, int& lineNo, double& length)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;
	int iPart = part - 1, iContour = contour - 1, iCode = element - 1;
	if (0 > iPart || pContainer->camData.num_Parts <= iPart)
	{
		ASSERT(FALSE);
		return CVERR_INVALID_PART_NO;
	}
	int iShape = pContainer->camData.pPart[iPart].iCAMShape;
	CAM_SHAPE& camShape = pContainer->camData.pCAMShape[iShape];
	if (0 > iContour || camShape.numContours <= iContour)
	{
		ASSERT(FALSE);
		return CVERR_INVALID_CONTOUR_NO;
	}
	CAM_CONTOUR& c = camShape.pContour[iContour];
	if (0 > iCode)
	{
		lineNo = c.iCmdBlockStr + 1;	// the first line of the contour
		length = 0;
	}
	else if (c.numCodes <= iCode)
	{
		lineNo = c.iCmdBlockStrLast + 1;	// the last line of the contour
		length = 0;
	}
	else
	{
		CAM_CODE& code = c.pCode[iCode];
		double x1, y1;
		if (0 == iCode)
		{
			x1 = c.x_start;
			y1 = c.y_start;
		}
		else
		{
			x1 = c.pCode[iCode-1].X;
			y1 = c.pCode[iCode-1].Y;
		}
		lineNo = code.nLineNo;
		length = code.CalcLength(x1, y1);
	}
	return CV_NOERROR;
}

int CAMViewerFactory::GetElementPos(HWND hWnd, int part, int contour, int element, double progress, double& wcsX, double& wcsY)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;
	int iPart = part - 1, iContour = contour - 1, iCode = element - 1;
	if (0 > iPart || pContainer->camData.num_Parts <= iPart)
	{
		ASSERT(FALSE);
		return CVERR_INVALID_PART_NO;
	}
	int iShape = pContainer->camData.pPart[iPart].iCAMShape;
	CAM_SHAPE& camShape = pContainer->camData.pCAMShape[iShape];
	if (0 > iContour || camShape.numContours <= iContour)
	{
		ASSERT(FALSE);
		return CVERR_INVALID_CONTOUR_NO;
	}
	CAM_CONTOUR& c = camShape.pContour[iContour];
	if (0 > iCode || c.numCodes <= iCode)
		return CVERR_INVALID_BLOCK_INDEX;

	double length = 0;
	CAM_CODE& code = c.pCode[iCode];
	double x1, y1;
	if (0 == iCode)
	{
		x1 = c.x_start;
		y1 = c.y_start;
	}
	else
	{
		x1 = c.pCode[iCode-1].X;
		y1 = c.pCode[iCode-1].Y;
	}

	progress = min(max(0, progress), 1.0);
	double I(0), J(0);
	if (!code.CalcPos(x1, y1, progress, wcsX, wcsY, I, J))
		return CVERR_INVALID_BLOCK_INDEX;

	wcsX += pContainer->camData.pPart[iPart].origin_X;
	wcsY += pContainer->camData.pPart[iPart].origin_Y;

	return CV_NOERROR;
}

int CAMViewerFactory::GetBlockProgress(HWND hWnd, int partNo, int contourNo, int lineNo, double wcsX, double wcsY, double& cutDone, double& cutRemains)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;
	return pContainer->GetBlockProgress(partNo-1, contourNo-1, lineNo, wcsX, wcsY, cutDone, cutRemains);
}

int CAMViewerFactory::GetElementBlockCode(HWND hWnd, int part, int contour, int element, char* pszBlock, int maxbuffer)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;
	return pContainer->viewer.GetElementBlockCode(part, contour, element, pszBlock, maxbuffer);
}

int CAMViewerFactory::GetBlockString(HWND hWnd, int lineNo, LPTSTR pszBlock, int bufferLength)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;
	return pContainer->GetBlockString(lineNo, pszBlock, bufferLength);
}

int CAMViewerFactory::GetMPFContent(HWND hWnd, CStringArray& arrLine)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;
	return pContainer->GetFileContent(arrLine);
}

int CAMViewerFactory::GetScanCut(HWND hWnd, int& scancut)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;
	return pContainer->viewer.GetScanCut(scancut);
}

int CAMViewerFactory::FinishCutting(HWND hWnd)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.FinishCuttingProgress();
	return CV_NOERROR;
}

int CAMViewerFactory::CompleteLastElement(HWND hWnd)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.CompleteLastElement();
	return CV_NOERROR;
}

int CAMViewerFactory::CompleteLastContour(HWND hWnd)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.CompleteLastContour();
	return CV_NOERROR;
}

int CAMViewerFactory::StopCutting(HWND hWnd)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.StopCuttingProgress();
	return CV_NOERROR;
}

int CAMViewerFactory::ResetCutting(HWND hWnd)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.ResetCuttingProress();
	return CV_NOERROR;
}

int CAMViewerFactory::SetCuttingDone(HWND hWnd, int fromPart, int fromContour, int toPart, int toContour, int isReverse)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.SetCuttingProressDone(fromPart, fromContour, toPart, toContour, (isReverse != 0));
	return CV_NOERROR;
}

int CAMViewerFactory::SetCuttingProgressDone(HWND hWnd, int fromPart, int fromContour, int toPart, int toContour, int toElement, double toProgress)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.SetCuttingProgressDone(fromPart, fromContour, toPart, toContour, toElement, toProgress);
	return CV_NOERROR;
}

int CAMViewerFactory::GetCuttingProgressInfo(HWND hWnd, CutProgress*& pInfo, int& nCount)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.GetCuttingProgressInfo(pInfo, nCount);
	return CV_NOERROR;
}

int CAMViewerFactory::GetCurrCutDistance(HWND hWnd, double& distance)
{
	CAMContainer* pContainer = FindContainer(hWnd);
	if (!pContainer)
		return CVERR_WINDOW_NOTFOUND;

	pContainer->viewer.GetCurrCutDistance(distance);
	return CV_NOERROR;
}



//////////////////////////////////////////////////////////////////////////
// class CAMContainer

int CAMContainer::GetJumpInfoString(int iPart, FlyingParam& param, bool inches, fileVersion ver, CString& strJumpParam)
{
	ASSERT(0 < iPart);

	int		nP1, nC1, nP2, nC2;
	Point2d pt1, pt2;

	if (!GetPartEndInfo(iPart-1, nP1, nC1, pt1.x, pt1.y)
		|| !GetPartStartInfo(iPart, nP2, nC2, pt2.x, pt2.y))
	{
		strJumpParam.Format(_T("Can't get the Part#%d end-position, Part#%d start-position"), iPart, iPart+1);
		return CVERR_CAMDATA;
	}

	switch (ver)
	{
	case verV08:
		getJumpSpotTypeV08(nP1, nC1, nP2, nC2, param);
		break;
	default:
		getJumpSpotTypeV16(nP1, nC1, nP2, nC2, param);
		break;
	}

	double	h1, h2, r;
	FlyingProfile jump;
	int nRet = generateFlyingMode(param, jump, inches, pt1, pt2, h1, h2, r);
	if (PROFILE_SUCCESS != nRet)
	{
		nRet = ErrCode_PROF2CAM(nRet);
		strJumpParam.Format(_T("Can't apply flying mode to the object#%d: error code(%d)"), iPart+1, nRet);
		return nRet;
	}
	int nJumpType = jump.getJumpType();

	CString strFloats;
	getJumpFloatString(h1, h2, r, inches, strFloats, nJumpType);
	strJumpParam.Format(_T(",%s,%d)\n"), strFloats, nJumpType);

	return CV_NOERROR;
}

int CAMContainer::GetJumpInfoString(int iPart, int iCont, FlyingParam& param, bool inches, fileVersion ver, CString& strJumpParam)
{
	ASSERT(0 < iCont);

	Point2d	  pt1, pt2;
	CAM_SHAPE* pPart = camData.pCAMShape;

	if (!pPart[iPart].pContour[iCont-1].GetEndPos(pt1.x, pt1.y))
	{
		strJumpParam.Format(_T("Can't get the object#%d-contour#%d end-position"), iPart+1, iCont+1);
		return CVERR_CAMDATA;
	}

	pt2.x = pPart[iPart].pContour[iCont].x_start;
	pt2.y = pPart[iPart].pContour[iCont].y_start;

	int nP1 = pPart[iPart].pContour[iCont-1].nPiercing;
	int nC1 = pPart[iPart].pContour[iCont-1].nCutting;
	int nP2 = pPart[iPart].pContour[iCont].nPiercing;
	int nC2 = pPart[iPart].pContour[iCont].nCutting;

	switch (ver)
	{
	case verV08:
		getJumpSpotTypeV08(nP1, nC1, nP2, nC2, param);
		break;
	default:
		getJumpSpotTypeV16(nP1, nC1, nP2, nC2, param);
		break;
	}

	double	h1, h2, r;
	FlyingProfile jump;
	int nRet = generateFlyingMode(param, jump, inches, pt1, pt2, h1, h2, r);
	if (PROFILE_SUCCESS != nRet)
	{
		nRet = ErrCode_PROF2CAM(nRet);
		strJumpParam.Format(_T("Can't apply flying mode to the object#%d-contour#%d: error code(%d)"), iPart+1, iCont+1, nRet);
		return nRet;
	}
	int nJumpType = jump.getJumpType();

	CString strFloats;
	getJumpFloatString(h1, h2, r, inches, strFloats, nJumpType);
	strJumpParam.Format(_T(",%s,%d)\n"), strFloats, nJumpType);

	return CV_NOERROR;
}

int CAMContainer::GetBlockString(int lineNo, LPTSTR pszBlock, int bufferLength)
{
	if (1 > bufferLength || !AfxIsValidAddress(pszBlock, bufferLength*sizeof(TCHAR)))
	{
		ASSERT(FALSE);
		return CVERR_INVALID_ARGS;
	}
	CString strBlock;
	if (!loader.getLineText(lineNo-1, strBlock))
		return CVERR_INVALID_LINE_NO;

	if (strBlock.GetLength() > bufferLength-1)
		return CVERR_BLOCK_BUFFER_SIZE;

	_tcscpy_s(pszBlock, bufferLength, strBlock);
	return CV_NOERROR;
}

int CAMContainer::GetBlockProgress(int iPart, int iContour, int nLineNo, double wcsX, double wcsY, double& cutDone, double& cutRemains)
{	// iPart, iContour = zero based index, nLineNo = sequential number starting from 1
	if (0 > iPart || camData.num_Parts <= iPart)
	{
		ASSERT(FALSE);
		return CVERR_INVALID_PART_NO;
	}

	const CAM_SHAPE& camShape = camData.pCAMShape[camData.pPart[iPart].iCAMShape];
	if (0 > iContour || camShape.numContours <= iContour)
	{
		ASSERT(FALSE);
		return CVERR_INVALID_CONTOUR_NO;
	}

	const CAM_CONTOUR& contour = camShape.pContour[iContour];
	const CAM_CODE* pBlock = contour.pCode;
	for (int iBlock = 0; iBlock < contour.numCodes; ++iBlock)
	{
		if (pBlock[iBlock].nLineNo == nLineNo)
		{
			const camLayout& viewerCAM = viewer.GetCAMLayoutInfo();
			ASSERT(iPart < viewerCAM._num_parts && iContour < viewerCAM._part[iPart].num_contours);
			const camContour& viewerContour = viewerCAM._part[iPart].pContour[iContour];
			ASSERT(contour.numCodes == viewerContour._numElements);
			if (viewerContour._pElement[iBlock].PtOnPath(wcsX, wcsY, cutDone, cutRemains))
				return CV_NOERROR;
			break;
		}
	}

	return CVERR_INVALID_LINE_NO;
}

int CAMContainer::GetFileContent(CStringArray& arrLine)
{
	if (!loader.getWholeContent(arrLine))
		return CVERR_NOCONTENTS;
	return CV_NOERROR;
}

int CAMContainer::WriteFlyingOptimization(const JumpParam& cfg, FileWriter& file, CString& strErr)
{
	double cutHeight = max(cfg.zCuttingGapNormal, max(cfg.zCuttingGapPulse, cfg.zCuttingGapHMISetting));
	if (LE(cfg.zJumpHeight, cutHeight) || LT(cfg.zJumpHeight, cfg.zPiercingGap))
	{
		strErr =_T("Some of the jump paramter heights is(are) too low.");
		return CVERR_FLYING_JUMPHEIGHTS;
	}

	FlyingParam param;
	initFlyingParamMetrics(cfg, param);

	loader.rewind();

	CString str, strNew, strFloats;

	fileVersion ver = camData.version;
	CAM_PART* pPartInfo = camData.pPart;
	CAM_SHAPE* pPart = camData.pCAMShape;

	bool inches = (MEASURES_INCHES == cfg.measureSystem);
	int nRet;

	for (int i = 1; i < camData.num_Parts; ++i)
	{
		// Preceding lines
		if (!loader.write(pPartInfo[i].iCmdBlockStr - 1, file))
		{
			ASSERT(FALSE);
			strErr.Format(_T("No block command for part#%d (block index = %d)"), i+1, pPartInfo[i].iCmdBlockStr);
			return CVERR_CAMDATA;
		}

		// Get the 'HKOST()' block
		if (!loader.feed(str))
		{
			strErr.Format(_T("No HKOST() for part#d"), i+1);
			return CVERR_CAMDATA;
		}

		int iHKOST = str.Find(_T("HKOST"));
		if (0 > iHKOST)
		{
			strErr.Format(_T("Block string mismatch for HKOST() at part#%d"), i+1);
			return CVERR_CAMDATA;
		}
		file.WriteString(str.Left(iHKOST));
		getFloatString(pPartInfo[i].origin_X, pPartInfo[i].origin_Y, pPartInfo[i].origin_R, inches, strFloats);
		strNew.Format(_T("HKOST(%s,%d,%d"),
					  strFloats, pPartInfo[i].nCAMpart_BlockNo, pPartInfo[i].nContoursInPart);
		file.WriteString(strNew);

		// append jump information
		nRet = GetJumpInfoString(i, param, inches, ver, strNew);
		if (CV_NOERROR != nRet)
		{
			strErr = strNew;
			return nRet;
		}
		file.WriteString(strNew);
	}

	CString strFloats2;
	for (int i = 0; i < camData.num_Shapes; ++i)
	{
		CAM_CONTOUR& c = pPart[i].pContour[0];

		// Write through the preceeding lines
		if (!loader.write(c.iCmdBlockStr - 1, file))
		{
			ASSERT(FALSE);
			strErr.Format(_T("No block command for object#%d - contour#%d (block index = %d)"), i+1, 1, c.iCmdBlockStr);
			return CVERR_CAMDATA;
		}

		// Get the 'HKSTR()' block
		if (!loader.feed(str))
		{
			strErr.Format(_T("No HKSTR() for object#d - contour#%d"), i+1, 1);
			return CVERR_CAMDATA;
		}
		if (0 <= str.Find(_T("HKSCRC")))
		{
			file.WriteString(str+_T('\n'));
			continue;
		}

		int iHKSTR = str.Find(_T("HKSTR"));
		if (0 > iHKSTR)
		{
			strErr.Format(_T("Block string mismatch for HKSTR() at object#%d-contour#%d"), i+1, 1);
			return CVERR_CAMDATA;
		}
		file.WriteString(str.Left(iHKSTR));
		getFloatString(c.x_start, c.y_start, inches, strFloats);
		getFloatString(c.width, c.height, inches, strFloats2);
		strNew.Format(_T("HKSTR(%d,%d,%s,%d,%s, 0, 0, 0)\n"),
					  c.nPiercing, c.nCutting, strFloats, c.nToolComp, strFloats2);
		file.WriteString(strNew);

		for (int j = 1; j < pPart[i].numContours; ++j)
		{
			// Preceeding lines
			CAM_CONTOUR& c = pPart[i].pContour[j];
			if (!loader.write(c.iCmdBlockStr - 1, file))
			{
				ASSERT(FALSE);
				strErr.Format(_T("No block command for object#%d - contour#%d (block index = %d)"), i+1, j+1, c.iCmdBlockStr);
				return CVERR_CAMDATA;
			}

			// Get the 'HKSTR()' block
			if (!loader.feed(str))
			{
				strErr.Format(_T("No HKSTR() for object#d - contour#%d"), i+1, j+1);
				return CVERR_CAMDATA;
			}
			int iHKSTR = str.Find(_T("HKSTR"));
			if (0 > iHKSTR)
			{
				strErr.Format(_T("Block string mismatch for HKSTR() at object#%d-contour#%d"), i+1, j+1);
				return CVERR_CAMDATA;
			}
			file.WriteString(str.Left(iHKSTR));
			getFloatString(c.x_start, c.y_start, inches, strFloats);
			getFloatString(c.width, c.height, inches, strFloats2);
			strNew.Format(_T("HKSTR(%d,%d,%s,%d,%s"),
						  c.nPiercing, c.nCutting, strFloats, c.nToolComp, strFloats2);
			file.WriteString(strNew);

			// Jump information
			nRet = GetJumpInfoString(i, j, param, inches, ver, strNew);
			if (CV_NOERROR != nRet)
			{
				strErr = strNew;
				return nRet;
			}
			file.WriteString(strNew);
		}
	}

	if (!loader.writeToEnd(file))
	{
		strErr = _T("Internal error: MPF file buffer is corrupted");
		return CVERR_CAMDATA;	// no error returns on file-writing but in parsing
	}

	return CV_NOERROR;
}

int CAMContainer::WriteRPPOptimization(FileWriter& file, CString& strErr)
{
	fileVersion ver = camData.version;
	CAM_PART* pPartInfo = camData.pPart;

	loader.rewind();

	CAM_SHAPE* pPart = camData.pCAMShape;
	Point2d pt1, pt2, pt3;
	CString str, strNew;

	for (int i = 0; i < camData.num_Shapes; ++i)
	{
		for (int j = 0; j < pPart[i].numContours; ++j)
		{
			CAM_CONTOUR& c = pPart[i].pContour[j];

			// Write preceeding lines
			if (!loader.write(c.iCmdBlockStr - 1, file))
			{
				ASSERT(FALSE);
				strErr.Format(_T("No block command for part#%d-contour#%d"), i+1, j+1);
				return CVERR_CAMDATA;
			}

			// Get the 'HKSTR()' block
			if (!loader.feed(str))
			{
				strErr.Format(_T("No HKSTR() for object#d-contour#%d"), i+1, j+1);
				return CVERR_CAMDATA;
			}
			file.WriteString(str+_T('\n'));

			if (0 <= str.Find(_T("HKSTR")))
			{
				// Write HKPIE() - HKLEA() - HKCUT() with dwell time optimization
				if (!writeThroughHKCUT(loader, c, file, strErr))
					return CVERR_CAMDATA;	// no error returns on file-writing but in parsing
			}
			else if (0 <= str.Find(_T("HKSCRC")))
			{
				if (!writeHKSCRCBlocks(loader, file, strErr))
					return CVERR_CAMDATA;	// no error returns on file-writing but in parsing
			}
		}
	}

	if (!loader.writeToEnd(file))
	{
		strErr = _T("Internal error: MPF file buffer is corrupted");
		return CVERR_CAMDATA;	// no error returns on file-writing but in parsing
	}

	return CV_NOERROR;
}

int CAMContainer::WriteFlyingRPPOptimization(const JumpParam& cfg, FileWriter& file, CString& strErr)
{
	double cutHeight = max(cfg.zCuttingGapNormal, max(cfg.zCuttingGapPulse, cfg.zCuttingGapHMISetting));
	if (LE(cfg.zJumpHeight, cutHeight) || LT(cfg.zJumpHeight, cfg.zPiercingGap))
	{
		strErr =_T("Some of the jump paramter heights is(are) too low.");
		return CVERR_FLYING_JUMPHEIGHTS;
	}

	FlyingParam   param;
	initFlyingParamMetrics(cfg, param);

	loader.rewind();

	CString str, strNew, strFloats;

	fileVersion ver = camData.version;
	CAM_PART* pPartInfo = camData.pPart;
	CAM_SHAPE* pPart = camData.pCAMShape;

	bool inches = (MEASURES_INCHES == cfg.measureSystem);
	int nRet;

	for (int i = 1; i < camData.num_Parts; ++i)
	{
		// Preceding lines
		//
		if (!loader.write(pPartInfo[i].iCmdBlockStr - 1, file))
		{
			ASSERT(FALSE);
			strErr.Format(_T("No block command for part#%d (block index = %d)"), i+1, pPartInfo[i].iCmdBlockStr);
			return CVERR_CAMDATA;
		}

		// Get the 'HKOST()' block
		if (!loader.feed(str))
		{
			strErr.Format(_T("No HKOST() for part#d"), i+1);
			return CVERR_CAMDATA;
		}
		int iHKOST = str.Find(_T("HKOST"));
		if (0 > iHKOST)
		{
			strErr.Format(_T("Block string mismatch for HKOST() at part#%d"), i+1);
			return CVERR_CAMDATA;
		}
		file.WriteString(str.Left(iHKOST));
		getFloatString(pPartInfo[i].origin_X, pPartInfo[i].origin_Y, pPartInfo[i].origin_R, inches, strFloats);
		strNew.Format(_T("HKOST(%s,%d,%d"),
					  strFloats, pPartInfo[i].nCAMpart_BlockNo, pPartInfo[i].nContoursInPart);
		file.WriteString(strNew);

		// append jump information
		nRet = GetJumpInfoString(i, param, inches, ver, strNew);
		if (CV_NOERROR != nRet)
		{
			strErr = strNew;
			return nRet;
		}
		file.WriteString(strNew);
	}

	CString strFloats2;
	for (int i = 0; i < camData.num_Shapes; ++i)
	{
		CAM_CONTOUR& c = pPart[i].pContour[0];

		// Write the preceeding lines, if any
		if (!loader.write(c.iCmdBlockStr - 1, file))
		{
			ASSERT(FALSE);
			strErr.Format(_T("No block command for object#%d - contour#%d (block index = %d)"), i+1, 1, c.iCmdBlockStr);
			return CVERR_CAMDATA;
		}

		// Get the 'HKSTR()' block
		if (!loader.feed(str))
		{
			strErr.Format(_T("No HKSTR() for object#d-contour#%d"), i+1, 1);
			return CVERR_CAMDATA;
		}
		if (0 <= str.Find(_T("HKSCRC")))
		{
			file.WriteString(str+_T('\n'));
			continue;
		}

		int iHKSTR = str.Find(_T("HKSTR"));
		if (0 > iHKSTR)
		{
			strErr.Format(_T("Block string mismatch for HKSTR() at object#%d-contour#%d"), i+1, 1);
			return CVERR_CAMDATA;
		}
		file.WriteString(str.Left(iHKSTR));
		getFloatString(c.x_start, c.y_start, inches, strFloats);
		getFloatString(c.width, c.height, inches, strFloats2);
		strNew.Format(_T("HKSTR(%d,%d,%s,%d,%s, 0, 0, 0)\n"),
					  c.nPiercing, c.nCutting, strFloats, c.nToolComp, strFloats2);
		file.WriteString(strNew);

		if (!writeThroughHKCUT(loader, c, file, strErr))
			return CVERR_CAMDATA;	// no error returns on file-writing but in parsing

		for (int j = 1; j < pPart[i].numContours; ++j)
		{
			// Preceding lines
			CAM_CONTOUR& c = pPart[i].pContour[j];

			// Write preceeding lines, if any
			if (!loader.write(c.iCmdBlockStr - 1, file))
			{
				ASSERT(FALSE);
				strErr.Format(_T("No block command for object#%d - contour#%d (block index = %d)"), i+1, j+1, c.iCmdBlockStr);
				return CVERR_CAMDATA;
			}

			// Get the 'HKSTR()' block
			if (!loader.feed(str))
				return CVERR_CAMDATA;
			int iHKSTR = str.Find(_T("HKSTR"));
			if (0 > iHKSTR)
			{
				strErr.Format(_T("Block string mismatch for HKSTR() at object#%d-contour#%d"), i+1, j+1);
				return CVERR_CAMDATA;
			}
			file.WriteString(str.Left(iHKSTR));
			getFloatString(c.x_start, c.y_start, inches, strFloats);
			getFloatString(c.width, c.height, inches, strFloats2);
			strNew.Format(_T("HKSTR(%d,%d,%s,%d,%s"),
						  c.nPiercing, c.nCutting, strFloats, c.nToolComp, strFloats2);
			file.WriteString(strNew);

			// append jump information
			nRet = GetJumpInfoString(i, j, param, inches, ver, strNew);
			if (CV_NOERROR != nRet)
			{
				strErr = strNew;
				return nRet;
			}
			file.WriteString(strNew);

			if (!writeThroughHKCUT(loader, c, file, strErr))
				return CVERR_CAMDATA;	// no error returns on file-writing but in parsing
		}
	}

	if (!loader.writeToEnd(file))
	{
		strErr = _T("Internal error: MPF file buffer is corrupted");
		return CVERR_CAMDATA;	// no error returns on file-writing but in parsing
	}

	return CV_NOERROR;
}

bool CAMContainer::WriteFlyingJumpTrace(const JumpParam& cfg, UINT ms_sampling, FileWriter& file, CString& strErr)
{
	double cutHeight = max(cfg.zCuttingGapNormal, max(cfg.zCuttingGapPulse, cfg.zCuttingGapHMISetting));
	if (LE(cfg.zJumpHeight, cutHeight) || LE(cfg.zJumpHeight, cfg.zPiercingGap))
	{
		strErr =_T("Some of the jump paramter heights is(are) too low.");
		return false;
	}

	FlyingParam		param;
	FlyingProfile	flying;
	FlyingTrace		buf;

	initFlyingParamMetrics(cfg, param);

	buf.ms_sampling = ms_sampling;

	CString str;
	double h1, h2, r;
	Point2d pt1, pt2;
	int nP1, nC1, nP2, nC2;

	loader.rewind();

	file.WriteString(_T("block cmd , time , jerkH , accelH , feedH , distH , jerkV , accelV , feedV , distV,\n"));

	fileVersion ver = camData.version;
	bool isInches = (MEASURES_INCHES == cfg.measureSystem);

	CAM_PART* pPartInfo = camData.pPart;
	for (int i = 0; i < camData.num_Parts; ++i)
	{
		CAM_PART& partInfo = camData.pPart[i];
		CAM_SHAPE& part = camData.pCAMShape[partInfo.iCAMShape];

		if (0 < i)
		{
			if (!GetPartEndInfo(i-1, nP1, nC1, pt1.x, pt1.y)
				|| !GetPartStartInfo(i, nP2, nC2, pt2.x, pt2.y))
				return false;

			switch (ver)
			{
			case verV08:
				getJumpSpotTypeV08(nP1, nC1, nP2, nC2, param);
				break;
			default:
				getJumpSpotTypeV16(nP1, nC1, nP2, nC2, param);
				break;
			}

			generateFlyingMode(param, flying, isInches, pt1, pt2, h1, h2, r);
			if (flying.trace(buf) && loader.getLineText(partInfo.iCmdBlockStr, str))
			{
				writeTraceBuf(buf, file, str);
			}
		}

		for (int j = 1; j < part.numContours; ++j)
		{
			CAM_CONTOUR& c1 = part.pContour[j-1];
			CAM_CONTOUR& c2 = part.pContour[j];

			if (!c1.GetEndPos(pt1.x, pt1.y))
				return false;
			pt2.x = c2.x_start, pt2.y = c2.y_start;
			nP1 = c1.nPiercing, nC1 = c1.nCutting;
			nP2 = c2.nPiercing, nC2 = c2.nCutting;

			switch (ver)
			{
			case verV08:
				getJumpSpotTypeV08(nP1, nC1, nP2, nC2, param);
				break;
			default:
				getJumpSpotTypeV16(nP1, nC1, nP2, nC2, param);
				break;
			}

			generateFlyingMode(param, flying, isInches, pt1, pt2, h1, h2, r);
			if (flying.trace(buf) && loader.getLineText(c2.iCmdBlockStr, str))
			{
				writeTraceBuf(buf, file, str);
			}
		}
	}

	return true;
}

bool CAMContainer::GetPartStartInfo(int iPart, int& piercing, int& cutting, double& X, double& Y)
{
	if (0 > iPart || camData.num_Parts <= iPart)
	{
		ASSERT(FALSE);
		return false;
	}
	CAM_PART& workPiece = camData.pPart[iPart];
	if (0 > workPiece.iCAMShape || camData.num_Shapes <= workPiece.iCAMShape)
	{
		ASSERT(FALSE);
		return false;
	}

	CAM_SHAPE& part = camData.pCAMShape[workPiece.iCAMShape];
	piercing = part.pContour[0].nPiercing;
	cutting = part.pContour[0].nCutting;

	if (IsZero(workPiece.origin_R))
	{
		X = workPiece.origin_X + part.pContour[0].x_start;
		Y = workPiece.origin_Y +  part.pContour[0].y_start;
	}
	else
	{
		double rad = Deg2Rad(workPiece.origin_R);
		double cos0 = cos(rad), sin0 = sin(rad);
		double x = part.pContour[0].x_start;
		double y = part.pContour[0].y_start;

		X = (x*cos0 - y*sin0) + workPiece.origin_X;
		Y = (x*sin0 + y*cos0) + workPiece.origin_Y;
	}

	return true;
}

bool CAMContainer::GetPartEndInfo(int iPart, int& piercing, int& cutting, double& X, double& Y)
{
	if (0 > iPart || camData.num_Parts <= iPart)
	{
		ASSERT(FALSE);
		return false;
	}

	CAM_PART& workPiece = camData.pPart[iPart];
	CAM_SHAPE& part = camData.pCAMShape[workPiece.iCAMShape];
	CAM_CONTOUR& lastC = part.pContour[part.numContours-1];

	piercing = lastC.nPiercing;
	cutting = lastC.nCutting;

	double x, y;

	if (0 < workPiece.nDetours && AfxIsValidAddress(workPiece.pDetour, sizeof(CAM_CODE)*workPiece.nDetours))
	{
		CAM_CODE& last = workPiece.pDetour[workPiece.nDetours-1];
		x = last.X, y = last.Y;
	}
	else
	{
		if (0 < lastC.num_detours
			&& AfxIsValidAddress(lastC.pDetour, sizeof(CAM_CODE)*lastC.num_detours))
		{
			CAM_CODE& last = lastC.pDetour[lastC.num_detours-1];
			x = last.X, y = last.Y;
		}
		else
		{
			CAM_CODE& last = lastC.pCode[lastC.numCodes-1];
			x = last.X, y = last.Y;
		}
	}

	if (IsZero(workPiece.origin_R))
	{
		X = x + workPiece.origin_X;
		Y =	y + workPiece.origin_Y;
	}
	else
	{
		double rad = Deg2Rad(workPiece.origin_R);
		double cos0 = cos(rad), sin0 = sin(rad);
		X = (x*cos0 - y*sin0) + workPiece.origin_X;
		Y = (x*sin0 + y*cos0) + workPiece.origin_Y;
	}

	return true;
}


void CAMContainer::ArrangeRestartFile()
{
	if (loader.isRestartFile())
	{
		RESTART_LOG& resLog = loader.getRestartInfo();
		ASSERT(1 <= resLog.partNo && resLog.modShapeNBlockNo/10000 == camData.num_Shapes);

		int iResPart = resLog.partNo - 1;
		int iOrgShape = resLog.shapeNBlockNo /10000 - 1;
		ASSERT(iOrgShape < camData.num_Shapes && resLog.shapeNBlockNo == camData.pCAMShape[iOrgShape].nBlockIdNo);
		if (0 > iOrgShape || iOrgShape >= camData.num_Shapes)
		{
			CAMViewerFactory::writeRestartLog(
				_T("warning: restart log found with invalid CAM shape block number (N%d) => shape index(=%d)\n"),
				resLog.shapeNBlockNo, iOrgShape);
			return;
		}
		if (resLog.shapeNBlockNo != camData.pCAMShape[iOrgShape].nBlockIdNo)
		{
			CAMViewerFactory::writeRestartLog(
				_T("CAM Shape block number mismatch: log = N%d, CAMshape = N%d\n"),
				resLog.shapeNBlockNo, camData.pCAMShape[iOrgShape].nBlockIdNo);
			return;
		}

		if (resLog.shapeNBlockNo == resLog.modShapeNBlockNo)
		{	// part/contour restart
			ASSERT(camData.pPart[iResPart].iCAMShape == iOrgShape);
			ASSERT(camData.pPart[iResPart].nCAMpart_BlockNo == camData.pCAMShape[iOrgShape].nBlockIdNo);
			int iContour = max(0, resLog.contourNo - 1);
			const CAM_CONTOUR& contour = camData.pCAMShape[camData.pPart[iResPart].iCAMShape].pContour[iContour];
			resLog.wcsX = contour.x_start;
			resLog.wcsY = contour.y_start;
		}
		else
		{
			CAM_PART& resSrcPart = camData.pPart[iResPart];
			const CAM_SHAPE& orgShape = camData.pCAMShape[iOrgShape];

			CString str;
			int iLine = camData.pPart[iResPart].iCmdBlockStr;
			loader.getLineText(iLine, str);
			if (replaceHKOSTBlockNo(str, resSrcPart.nCAMpart_BlockNo, orgShape.nBlockIdNo))
				loader.setLineText(iLine, str);
			resSrcPart.iCAMShape = iOrgShape;
			resSrcPart.nCAMpart_BlockNo = orgShape.nBlockIdNo;
			ASSERT(orgShape.numContours == resSrcPart.nContoursInPart);
			resSrcPart.nContoursInPart = orgShape.numContours;
		}

		CAMViewerFactory::writeRestartLog(
			_T("Restart log found at Part#%d, Contour#%d\n"),
			resLog.partNo, resLog.contourNo);
	}
	return;
}


int CAMContainer::WriteRestartWorkFile(const RestartInfo& param, FileWriter& file, CString& strErr)
{
	RESTART info;
	int nRC = restartGetRestartPos(param, info);
	if (CV_NOERROR != nRC)
	{
		strErr = info.strMarkUp;
		return nRC;
	}

	if (restartFile == info.pos)
	{
		return loader.copyTo(file)? CV_NOERROR: CVERR_FILECREATE;
	}

	loader.rewind();

	//
	// Write through the first block, and then the restart header mark-up
	//
	if (loader.isRestartFile())
	{
		const RESTART_LOG& log = loader.getRestartInfo();
		loader.write(log.iRestartStart - 1, file);
		loader.moveToLine(log.iRestartStart + log.nRestartLogLines + 1);	// add extra one blank line
	}
	loader.write(loader.getN1LineNo() - 2, file);	// N1 Line No. is 1-based line number
	file.WriteString(info.strMarkUp);

	//
	// Write the rest of contents according the restart positions
	//
	switch (info.pos)
	{
	case restartPart:	case restartPiercing:	case restartLeadIn:
		ASSERT(1 == info.contourNo || 0 == info.iBlockCode);
		loader.writeToEnd(file);
		break;
	case restartCutting:
		restartWriteModPart(info, file);
		break;
	default:
		ASSERT(FALSE);	// can't happen this case
		CAMViewerFactory::appendRestartLog(_T("Undefined restarting position: %u\n"), info.pos);
		return CVERR_RESTART_INTERNAL;
		break;

	}
	return CV_NOERROR;
}


int CAMContainer::restartGetRestartPos(const RestartInfo& param, RESTART& info)
{
	ASSERT(loader.getVersion() == camData.version);
	CString strLog;

	//
	// Make a clone of RestartInfo to make the integrity verified parameter set
	//
	info.clone(param);
	appendRestartInfoLog(param, strLog);
	if (loader.isRestartFile() && param.lineNo >= loader.getRestartInfo().modStartLine)
	{
		info.lineNo -= loader.getRestartInfo().offsetModLines;
		appendRestartModLineOffset(param, info, strLog);
	}

	//
	// First, get the line numbers of the source MPF file
	//
	const int nFirstNestingLine = camData.nestingStartLine();
	const int nLastNestingLine = camData.nestingEndLine();
	const int nFirstPartStartLine = camData.partStartLine();
	appendRestartLinesLog(camData.version, nFirstNestingLine, nLastNestingLine, nFirstPartStartLine, strLog);

	//
	// And then check whether the last line was in the nesting lines or in the CAM lines
	//
	int nRC = CV_NOERROR;
	if (info.lineNo <= nFirstNestingLine)	// then do RestartWork from the beginning of the file
	{
		addLogStrRestartFile(info, nFirstNestingLine, strLog);
		info.pos = restartFile;
		info.partNo = info.contourNo = 1;
	}
	else if (info.lineNo <= nLastNestingLine)	// then do RestartWork at the beginning of the part
	{
		addLogStrRestartAtNesting(info, nLastNestingLine, strLog);
		nRC = restartGetPosAtNesting(info, strLog);
	}
	else if (nFirstPartStartLine <= info.lineNo)	// then locate restart position inside the last part
	{
		addLogStrRestartBlock(info, nFirstPartStartLine, strLog);
		nRC = restartGetPosAtPart(info, strLog);
	}
	else	// somewhere between the nesting information and part shape information
	{
		info.strMarkUp = _T("The previous work already went through the last part. No more work to do");
		nRC = CVERR_RESTART_FINISHED;
		addLogStrRestartNone(strLog);
	}

	CAMViewerFactory::appendRestartLog(strLog);

	return nRC;
}


int CAMContainer::restartGetPosAtNesting(RESTART& info, CString& strLog)
{
	if (verV16A05 == loader.getVersion())
	{
		for (int i = 0; i < camData.num_Parts; ++i)
		{
			const int partLineNo = camData.pPart[i].iCmdBlockStr + 1;
			if (partLineNo == info.lineNo)
			{
				const CAM_PART& part = camData.pPart[i];
				const CAM_CONTOUR& contour = camData.pCAMShape[part.iCAMShape].pContour[0];
				makeRestartAtNesting(i + 1, part, contour, info, verV16A05, strLog);
				return CV_NOERROR;
			}
			else if (info.lineNo == partLineNo + 1)	// HKPPP
			{
				if (i + 1 == camData.num_Parts)	// the last part
					return CVERR_RESTART_FINISHED;
				CString str;
				loader.getLineText(camData.pPart[i].iCmdBlockStr + 1, str);
				ASSERT(0 <= str.Find(_T("HKPPP")));
				appendRestartLog(strLog, _T("* Restart position changed to the starting position of the next part\n"));
				appendRestartLog(strLog, _T("  because it was stopped at HKPPP (%s)\n"), str);

				const CAM_PART& part = camData.pPart[i + 1];
				const CAM_CONTOUR& contour = camData.pCAMShape[part.iCAMShape].pContour[0];
				makeRestartAtNesting(i + 2, part, contour, info, verV16A05, strLog);
				return CV_NOERROR;
			}
		}
	}
	else
	{
		for (int i = 0; i < camData.num_Parts; ++i)
		{
			const int partLineNo = camData.pPart[i].iCmdBlockStr + 1;
			if (partLineNo == info.lineNo)
			{
				const CAM_PART& part = camData.pPart[i];
				const CAM_CONTOUR& contour = camData.pCAMShape[part.iCAMShape].pContour[0];
				makeRestartAtNesting(i+1, part, contour, info, loader.getVersion(), strLog);
				return CV_NOERROR;
			}
		}
	}

	info.strMarkUp.Format(_T(" Can't find nesting line has the line number %d\n"), info.lineNo);
	appendRestartLog(strLog,info.strMarkUp);

	return CVERR_RESTART_LINE_NO;
}


int CAMContainer::restartGetPosAtPart(RESTART& info, CString& strLog)
{
	if (info.lineNo < camData.partStartLine() || camData.partEndLine() < info.lineNo)
	{
		ASSERT(FALSE);
		info.strMarkUp = _T("Restart line number is out of range");
		appendRestartLog(strLog, _T("**Error CAMContainer::RestartWorkCheckPart() is called with invalid line number, %d\n"), info.lineNo);
		appendRestartLog(strLog, _T(" current MPF has part information over line#%d ~ %d\n"), camData.partStartLine(), camData.partEndLine());
		return CVERR_RESTART_LINE_NO;
	}

	//
	// Check the part number.
	//
	if (1 > info.partNo || info.partNo > camData.num_Parts)
	{
		info.strMarkUp = _T("Part number is out of range");
		appendRestartLog(strLog, _T("**Error, partNo(%d) out of range[1, %d]\n"), info.partNo, camData.num_Parts);
		appendRestartLog(strLog, _T("Error returns, CVERR_RESTART_PART_NO(%d)\n"), CVERR_RESTART_PART_NO);
		return CVERR_RESTART_PART_NO;
	}

	//
	// And then, the contour number.
	//
	CAM_PART& part = camData.pPart[info.partNo - 1];
	if (1 > info.contourNo || info.contourNo > part.nContoursInPart)
	{
		info.strMarkUp = _T("Contour number is out of range");
		appendRestartLog(strLog, _T("**Error, contourNo(%d) out of range[1, %d]\n"), info.contourNo, part.nContoursInPart);
		appendRestartLog(strLog, _T("Error returns, CVERR_RESTART_CONTOUR_NO(%d)\n"), CVERR_RESTART_CONTOUR_NO);
		return CVERR_RESTART_CONTOUR_NO;
	}
	ASSERT(0 <= part.iCAMShape && part.iCAMShape < camData.num_Shapes);

	const CAM_SHAPE& partShape = camData.pCAMShape[part.iCAMShape];

	info.orgShapeBlockNo = (part.iCAMShape + 1)*10000 + 1;

	if (loader.isRestartFile())
	{
		info.modShapeBlockNo = loader.getRestartInfo().modShapeNBlockNo;
		info.restartShapeLineNo = loader.getRestartInfo().modStartLine;
		info.orgModLineOffset = (info.restartShapeLineNo + 1) - (partShape.pContour[0].iCmdBlockStr + 1);
		info.restartContourLineNo = (partShape.pContour[info.contourNo - 1].iCmdBlockStr + 1) + info.orgModLineOffset;
	}
	else
	{
		info.modShapeBlockNo = (camData.num_Shapes + 1)*10000 + 1;
		info.restartShapeLineNo = loader.lines() + 2 + 6;
		info.orgModLineOffset = (loader.lines() + 3) - (partShape.pContour[0].iCmdBlockStr + 1);
		info.restartContourLineNo = (partShape.pContour[info.contourNo - 1].iCmdBlockStr + 1) + info.orgModLineOffset + 6;
	}
	ASSERT(part.nCAMpart_BlockNo == info.orgShapeBlockNo);

	//
	// Try to find the nearest line in the current contour of the part
	//
	int iLine = info.lineNo - 1;
	int iContour = info.contourNo - 1;
	const CAM_CONTOUR* pContour = partShape.pContour;
	const CAM_CONTOUR& contour = pContour[iContour];

	if (iLine < contour.iCmdBlockStr)
	{	// need to find the corresponding line
		appendRestartLog(strLog, _T("*Warning, the line number(%d) is before the current contour's block lines[%d ~ %d]\n"), iLine+1, contour.iCmdBlockStr+1, contour.iCmdBlockStrLast+1);
		for (int i = iContour - 1; 0 <= i; --i)
		{
			if (pContour[i].iCmdBlockStr <= iLine && iLine <= pContour[i].iCmdBlockStrLast)
			{
				appendRestartLog(strLog, _T("The line number(%d) belongs to the contourNo(%d)\n"), info.lineNo, i+1);
				info.contourNo = i + 1;
				return restartGetPosAtBlock(info, strLog);
			}
		}
	}
	else if (contour.iCmdBlockStrLast < iLine)
	{
		appendRestartLog(strLog, _T("*Warning, the line number(%d) is beyond the current contour[%d ~ %d]\n"), iLine+1, contour.iCmdBlockStr+1, contour.iCmdBlockStrLast+1);
		for (int i = iContour + 1; i < partShape.numContours; ++i)
		{
			if (pContour[i].iCmdBlockStr <= iLine && iLine <= pContour[i].iCmdBlockStrLast)
			{
				appendRestartLog(strLog, _T("The line number(%d) belongs to the contourNo(%d)\n"), info.lineNo, i+1);
				info.contourNo = i + 1;
				return restartGetPosAtBlock(info, strLog);
			}
		}
	}
	else // the line number falls inside the current contour
	{
		return restartGetPosAtBlock(info, strLog);
	}

	return CVERR_RESTART_LINE_NO;
}

int CAMContainer::restartGetPosAtBlock(RESTART& info, CString& strLog)
{
	CAM_PART& part = camData.pPart[info.partNo - 1];
	ASSERT(0 <= part.iCAMShape && part.iCAMShape < camData.num_Shapes);
	const CAM_SHAPE& partShape = camData.pCAMShape[part.iCAMShape];
	const CAM_CONTOUR& contour = partShape.pContour[info.contourNo - 1];

	ASSERT(contour.iCmdBlockStr <= info.lineNo - 1 && info.lineNo - 1 <= contour.iCmdBlockStrLast);

	CString str;
	loader.getLineText(part.iCmdBlockStr, str);
	if (!restartMakeHKOST(str, partShape.numContours, info))
	{
		ASSERT(FALSE);
		appendRestartLog(strLog, _T("Error in making restart HKOST\n"));
		return CVERR_RESTART_INTERNAL;
	}

	if (contour.pCode[0].nLineNo > info.lineNo)
	{
		info.iBlockCode = 0;
		info.modShapeBlockNo = info.orgShapeBlockNo;
		info.restartShapeLineNo = info.orgModLineOffset = 0;
		appendRestartLog(strLog, _T("Contour start lineNo(%d) <= restart lineNo(%d) < first block line No(%d)\n"),
						 contour.iCmdBlockStr+1, info.lineNo, contour.pCode[0].nLineNo);
		if (1 == info.contourNo)
		{
			info.pos = restartPart;
			info.wcsX = part.origin_X;
			info.wcsY = part.origin_Y;
			appendRestartLog(strLog, _T("Restart contourNo(%d) => restart position = retartPart\n"), info.contourNo);
			restartBuildMarkupText(loader.getVersion(), info);
		}
		else
		{
			info.pos = contour.HasPiercing()? restartPiercing: restartLeadIn;
			info.wcsX = part.origin_X + contour.x_start;
			info.wcsY = part.origin_Y + contour.y_start;
			appendRestartLog(strLog, _T("Restart contourNo(%d) => restart position = %s\n"),
							 info.contourNo, (restartPiercing==info.pos)? _T("restartPiercing"): _T("restartLeadIn"));
			restartBuildMarkupText(loader.getVersion(), info);
		}
		return CV_NOERROR;
	}
	else
	{
		ASSERT(info.lineNo <= contour.pCode[contour.numCodes-1].nLineNo);

		for (int i = 0; i < contour.numCodes; ++i)
		{
			if (contour.pCode[i].nLineNo != info.lineNo)
				continue;

			info.iBlockCode = i;
			info.pos = restartCutting;
			restartBuildMarkupText(loader.getVersion(), info);
			double x1 = contour.x_start, y1 = contour.y_start;
			if (0 < i)
			{
				x1 = contour.pCode[i-1].X;
				y1 = contour.pCode[i-1].Y;
			}

			double lengthTotal = contour.pCode[i].CalcLength(x1, y1);
			double restart_x = info.wcsX - part.origin_X;
			double restart_y = info.wcsY - part.origin_Y;
			ASSERT(GE(restart_x, 0) && GE(restart_y, 0));

			double cutDone = contour.pCode[i].CalcLength(restart_x, restart_y) + info.startingOffset;
			if (0 > info.startingOffset)
			{
				cutDone = max(0, cutDone);
				ASSERT(GE(lengthTotal, cutDone));
				double ratio = cutDone / lengthTotal, I, J;
				contour.pCode[i].CalcPos(x1, y1, ratio, restart_x, restart_y, I, J);
			}
			else if (0 < info.startingOffset)
			{
				cutDone = min(cutDone, lengthTotal);
				double ratio = cutDone / lengthTotal, I, J;
				contour.pCode[i].CalcPos(x1, y1, ratio, restart_x, restart_y, I, J);
			}

			loader.getLineText(contour.iCmdBlockStr, str);
			if (!restartMakeHKSTR(str, 0, restart_x, restart_y, info))
			{
				ASSERT(FALSE);
				appendRestartLog(strLog, _T("Error in making restart HKSTR\n"));
				return CVERR_RESTART_INTERNAL;
			}

			return CV_NOERROR;
		}
		appendRestartLog(strLog, _T("* Can't find G-code block having line number(%d)\n"), info.lineNo);
		return CVERR_RESTART_LINE_NO;
	}
}


inline void restartWriteModContour(const CAM_CONTOUR& contour, int N_blockNo, FileMPF& loader, FileWriter& file)
{
	// if any, write through one line before the starting line of the modified contour
	loader.write(contour.iCmdBlockStr - 1, file);

	CString str;

	// HKSTR with the modified block number
	loader.feed(str);
	int iHKSTR = str.Find(_T("HKSTR"));
	if (0 > iHKSTR)
	{
		ASSERT(FALSE);
		return;
	}
	str.Format(_T("N%d %s\n"), N_blockNo, str.Mid(iHKSTR));
	file.WriteString(str);

	// Write through the end of the contour
	loader.write(contour.iCmdBlockStrLast, file);

	return;
}

int CAMContainer::restartWriteModPart(RESTART& info, FileWriter& file)
{
	CString str;

	ASSERT(restartCutting == info.pos && 1 <= info.contourNo && 1 <= info.partNo);
	const CAM_PART& nestingPart = camData.pPart[info.partNo - 1];
	const CAM_SHAPE& partShape = camData.pCAMShape[nestingPart.iCAMShape];
	ASSERT(partShape.nBlockIdNo == info.orgShapeBlockNo);

	//
	// Nesting information: HKOST's
	//

	loader.write(nestingPart.iCmdBlockStr - 1, file);	// Write through just prior to the modified part HKOST
	file.WriteString(info.strHKOST);					// the new HKOST of the modified part
	VERIFY(loader.next());								// forward loader line by skipping the old HKOST

	//
	// Write through rest of HKOST and the part-shape CAM data
	//       to the end of the original file content
	//
	if (loader.isRestartFile())
		loader.write(info.restartShapeLineNo - 3, file);
	else
		loader.writeToEnd(file);

	//
	// Add the modified restart part CAM shape information at the end of the file
	//

	// 1. Restart part mark-up head
	str.Format(_T("\n;%s\n"), csz_RestartPartHead);
	file.WriteString(str);

	// 2. Preceding contours
	loader.moveToLine(partShape.pContour[0].iCmdBlockStr);
	int N_blockNo = info.modShapeBlockNo;
	for (int i = 0; i < info.contourNo - 1; ++i, ++N_blockNo)
		restartWriteModContour(partShape.pContour[i], N_blockNo, loader, file);

	// 2. Modified contour
	const CAM_CONTOUR& contour = partShape.pContour[info.contourNo-1];
	loader.moveToLine(contour.iCmdBlockStr + 1);	// the very next to HKSTR()
	int nStartLineNo = contour.pCode[info.iBlockCode].nLineNo - 1;
	switch (loader.getVersion())
	{
	case verV16:
		file.WriteString(info.strHKSTR);
		ASSERT (FALSE);	// not fully implemented yet
		break;
	case verV16A05:
		file.WriteString(info.strHKSTR);
		while (loader.getLineNoToRead() < nStartLineNo)
		{
			loader.feed(str);
			if (IsCommentLine(str))
			{
				file.WriteString(str + '\n');
			}
			else if (0 <= str.Find(_T("HK")))
			{
				if (0 <= str.Find(_T("HKLEA")))
					file.WriteString(_T("HKLEA(0,0,0,0,0,0,0,0)\n"));
				else
					file.WriteString(str + '\n');
			}
			else
			{
				file.WriteString(_T(';') + str + _T('\n'));
			}
		}
		loader.write(contour.iCmdBlockStrLast, file);
		break;
	default:
		CAMViewerFactory::appendRestartLog(
			_T("Error in writing modified contour: invalid file version(%u)\n"),
			loader.getVersion());
		return CVERR_FILE_VERSION;
		break;
	}
	++N_blockNo;
	for (int i = info.contourNo; i < partShape.numContours; ++i, ++N_blockNo)
	{
		restartWriteModContour(partShape.pContour[i], N_blockNo, loader, file);
	}

	// 3. The rest of contours
	CString strPED;
	int iPriorToPED = partShape.nEndOfPartLineNo - 2;
	loader.write(iPriorToPED, file);
	loader.feed(str);
	int iPartEnd = str.Find(_T("HKPED"));
	if (0 >= iPartEnd)
	{
		ASSERT(FALSE);
		strPED = _T("Error in writing the end of part block: invalid block = ");
		strPED += str + _T('\n');
		CAMViewerFactory::appendRestartLog(strPED);
		return CVERR_RESTART_INTEGRITY;
	}
	strPED.Format(_T("N%d %s\n"), N_blockNo, str.Mid(iPartEnd));
	file.WriteString(strPED);

	// 4. Restart part mark-up tail 
	str.Format(_T(";%s\n"), csz_RestartPartTail);
	file.WriteString(str);

	return CV_NOERROR;
}

int CAMContainer::UpdateCuttingProgress(int nPart, int nContour, const int mpfLineNo, double progress, double xwcs, double ywcs)
{
	//
	// First, get the line numbers of the source MPF file to locate mpfLineNo
	//
	const int nFirstNestingLine = camData.nestingStartLine();
	const int nLastNestingLine = camData.nestingEndLine();
	const int nFirstPartStartLine = camData.partStartLine();

	if (mpfLineNo <= nFirstNestingLine)	// the current line does not get into the cutting yet
	{

	}
	else if (mpfLineNo <= nLastNestingLine)	// the current line falls inside the nesting zone
	{

	}
	else if (nFirstPartStartLine <= mpfLineNo) // the current line belongs to the CAM shape G-code blocks
	{
		;
	}
	else	// the current line belongs to somewhere between nesting information and CAM shape information 
	{
		;
	}

	return CV_NOERROR;
}
