#pragma once

inline bool	IsSameCAM(double a, double b)
{
	return (a > b)? (a-b < 1.0e-2): (b-a < 1.0e-2);
}

inline bool IsZeroStr(const CString& s)
{
	return (2 == s.GetLength() && _T('.') == s.GetAt(1));
}

inline bool IsNumber(TCHAR ch)
{
	return (_T('0') <= ch && ch <= _T('9'));
}

inline bool IsNumber(const CString& token, int iStart)
{
	int ccLen = token.GetLength();
	for (int i = iStart; i < ccLen; ++i)
	{
		if (!IsNumber(token[i]))
			return false;
	}
	return (iStart < ccLen);
}

inline bool IsNumber(const CString& token, int iStart, bool& isFloatingPoint)
{
	isFloatingPoint = false;
	int ccLen = token.GetLength();
	for (int i = iStart; i < ccLen; ++i)
	{
		if (!IsNumber(token[i]))
		{
			if (isFloatingPoint || _T('.') != token[i])
				return false;
			isFloatingPoint = true;
		}
	}
	return (iStart < ccLen);
}

inline bool GetNumber(const CString& token, int iStart, int& nValue)
{
	nValue = 0;
	int iNext = iStart, nLength = token.GetLength();
	for (; iNext < nLength && _T(' ') != token[iNext]; ++iNext)
	{
		if (IsNumber(token[iNext]))
			nValue = nValue*10 + (token[iNext]-_T('0'));
		else
			break;
	}
	return (iStart < iNext);
}

inline bool GetCAMNumber(const CString& token, int iStart, int& nValue)
{
	nValue = 0;

	int iNext = iStart, nLength = token.GetLength(), nCount = 0;
	for (; iNext < nLength; ++iNext)
	{
		TCHAR ch = token.GetAt(iNext);
		if (_T(' ') == ch || _T('=') == ch || _T('\t') == ch || _T('\r') == ch || _T('\n') == ch)
			continue;
		if (!IsNumber(ch))
			return false;
		break;
	}

	for (; iNext < nLength; ++iNext)
	{
		if (IsNumber(token[iNext]))
			nValue = nValue*10 + int(token.GetAt(iNext) - _T('0'));
		else
			break;
		++nCount;
	}
	return (0 < nCount);
}

inline bool GetCAMCoords(const CString& token, int iStart, double& value)
{
	value = 0;

	int iNext = iStart, nLength = token.GetLength(), nCount = 0;
	int nValue = 0, nSign = 1;
	double decimal = 0;
	for (; iNext < nLength; ++iNext)
	{
		TCHAR ch = token.GetAt(iNext);
		if (_T(' ') == ch || _T('=') == ch || _T('\t') == ch || _T('\r') == ch || _T('\n') == ch)
			continue;

		if (!IsNumber(ch))
		{
			if (_T('.') == ch)
				goto GetDecimals;
			else if (_T('-') != ch || ++iNext >= nLength)
				return false;

			ASSERT(_T('-') == ch);
			ch = token.GetAt(iNext);
			if (!IsNumber(ch))
				return false;

			nSign = -1;
		}
		break;
	}

	for (; iNext < nLength; ++iNext)
	{
		if (IsNumber(token[iNext]))
			nValue = nValue*10 + int(token.GetAt(iNext) - _T('0'));
		else
			break;
		++nCount;
	}
	if (iNext >= nLength || _T('.') != token[iNext])
		goto CompleteNumber;

GetDecimals:
	ASSERT(_T('.') == token[iNext]);

	double factor = 0.1;
	for (++iNext; iNext < nLength; ++iNext)
	{
		if (IsNumber(token[iNext]))
			decimal = decimal + int(token.GetAt(iNext) - _T('0'))*factor;
		else
			break;
		factor *= 0.1;
		++nCount;
	}

CompleteNumber:
	value = nSign*nValue + decimal;
	return (0 < nCount);
}

inline bool IsNumeric(const CString& str, int iStart=0, int* pDecimals=NULL)
{
	TCHAR ch = str[iStart];
	if (!IsNumber(ch) && !(_T('+') == ch || _T('-') == ch || _T('.') == ch))
		return false;

	int nDecimals = (_T('.') == ch)? 1: 0;
	for (int i = iStart+1; i < str.GetLength(); ++i)
	{
		if (_T('.') == str[i])
			++nDecimals;
		else if (!IsNumber(str[i]))
			return false;
	}
	if (pDecimals)
		*pDecimals = nDecimals;
	return (1 >= nDecimals);
}

inline bool IsNumeric(const CString& str, double& value, int iStart)
{
	if (IsNumeric(str, iStart))
	{
		value = (0 < iStart)? _tstof(str.Mid(iStart)): _tstof(str);
		return true;
	}
	return false;
}

inline bool GetGcodeAddress(const CString& strGcode, int& nAddr)
{
	return (_T('G') == strGcode.GetAt(0) && GetNumber(strGcode, 1, nAddr));
}
