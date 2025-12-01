#include "stdafx.h"
#include "HKCAMInterfaceDLL.h"
#include "VersionInfo.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif // _DEBUG

#pragma comment(lib, "Version.lib")

//////////////////////////////////////////////////////////////////////////

BOOL GetSystemErrorMsg(DWORD dwError, CString& strErr)
{
	LPVOID lpMsgBuf = NULL;
	DWORD cbLen = FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM |
								FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_IGNORE_INSERTS,
								NULL, dwError,
								MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
								(LPTSTR)&lpMsgBuf, 0, NULL);
	if (lpMsgBuf)
	{
		strErr = LPCTSTR(lpMsgBuf);
		LocalFree(lpMsgBuf);
		return TRUE;
	}
	return FALSE;
}


bool getAppVersions(CString& strProd, CString& strFile)
{
	TCHAR szModPath[MAX_PATH] ={ 0 };
	DWORD dwHandle = 0;
	size_t buf_size = 0;
	UINT   uLen = 0;

	VS_FIXEDFILEINFO* lpFfi = NULL;
	LPBYTE lpVersionInfo = NULL;

	DWORD dwSize = GetModuleFileName(NULL, szModPath, _countof(szModPath));
	if (0 == dwSize)
		goto ErrorReturn;

	dwSize = GetFileVersionInfoSize(szModPath, &dwHandle);
	if (0 == dwSize)
		goto ErrorReturn;

	buf_size = ((dwSize+2)/2)*2;
	lpVersionInfo = new BYTE[buf_size];
	memset(lpVersionInfo, 0, buf_size);
	if (!GetFileVersionInfo(szModPath, 0, buf_size, lpVersionInfo))
		goto ErrorReturn;

	if (VerQueryValue(lpVersionInfo, _T("\\"), (LPVOID *)&lpFfi, &uLen) && 0 < uLen)
	{
		strProd.Format(_T("%d.%d.%d.%d"),
					   HIWORD(lpFfi->dwProductVersionMS), LOWORD(lpFfi->dwProductVersionMS),
					   HIWORD(lpFfi->dwProductVersionLS), LOWORD(lpFfi->dwProductVersionLS));
		strFile.Format(_T("%d.%d.%d.%d"),
					  HIWORD(lpFfi->dwFileVersionMS), LOWORD(lpFfi->dwFileVersionMS),
					  HIWORD(lpFfi->dwFileVersionLS), LOWORD(lpFfi->dwFileVersionLS));
	}
	delete[] lpVersionInfo;

	return (0 < uLen);

ErrorReturn:
	GetSystemErrorMsg(GetLastError(), strProd);
	if (lpVersionInfo)
		delete[] lpVersionInfo;
	return false;
}


bool getAppVersions(stVersion& prodVer, stVersion& fileVer)
{
	TCHAR szModPath[MAX_PATH] ={ 0 };
	DWORD dwHandle = 0;
	size_t buf_size = 0;
	UINT   uLen = 0;

	VS_FIXEDFILEINFO* lpFfi = NULL;
	LPBYTE lpVersionInfo = NULL;

	DWORD dwSize = GetModuleFileName(NULL, szModPath, _countof(szModPath));
	if (0 == dwSize)
		goto ErrorReturn;

	dwSize = GetFileVersionInfoSize(szModPath, &dwHandle);
	if (0 == dwSize)
		goto ErrorReturn;

	buf_size = ((dwSize+2)/2)*2;
	lpVersionInfo = new BYTE[buf_size];
	memset(lpVersionInfo, 0, buf_size);
	if (!GetFileVersionInfo(szModPath, 0, buf_size, lpVersionInfo))
		goto ErrorReturn;

	if (VerQueryValue(lpVersionInfo, _T("\\"), (LPVOID *)&lpFfi, &uLen) && 0 < uLen)
	{
		prodVer.ver[0] = BYTE(HIWORD(lpFfi->dwProductVersionMS));
		prodVer.ver[1] = BYTE(LOWORD(lpFfi->dwProductVersionMS));
		prodVer.ver[2] = BYTE(HIWORD(lpFfi->dwProductVersionLS));
		prodVer.ver[3] = BYTE(LOWORD(lpFfi->dwProductVersionLS));
		fileVer.ver[0] = BYTE(HIWORD(lpFfi->dwFileVersionMS));
		fileVer.ver[1] = BYTE(LOWORD(lpFfi->dwFileVersionMS));
		fileVer.ver[2] = BYTE(HIWORD(lpFfi->dwFileVersionLS));
		fileVer.ver[3] = BYTE(LOWORD(lpFfi->dwFileVersionLS));
	}
	delete[] lpVersionInfo;

	return (0 < uLen);

ErrorReturn:
	if (lpVersionInfo)
		delete[] lpVersionInfo;
	return false;
}


bool getAppVersions(LPCTSTR pszFilePath, CString& strProd, CString& strFile)
{
	DWORD dwHandle = 0;
	size_t buf_size = 0;
	UINT   uLen = 0;

	VS_FIXEDFILEINFO* lpFfi = NULL;
	LPBYTE lpVersionInfo = NULL;

	DWORD dwSize = GetFileVersionInfoSize(pszFilePath, &dwHandle);
	if (0 == dwSize)
		goto ErrorReturn;

	buf_size = ((dwSize+2)/2)*2;
	lpVersionInfo = new BYTE[buf_size];
	memset(lpVersionInfo, 0, buf_size);
	if (!GetFileVersionInfo(pszFilePath, 0, buf_size, lpVersionInfo))
		goto ErrorReturn;

	if (VerQueryValue(lpVersionInfo, _T("\\"), (LPVOID *)&lpFfi, &uLen) && 0 < uLen)
	{
		strProd.Format(_T("%d.%d.%d.%d"),
					   HIWORD(lpFfi->dwProductVersionMS), LOWORD(lpFfi->dwProductVersionMS),
					   HIWORD(lpFfi->dwProductVersionLS), LOWORD(lpFfi->dwProductVersionLS));
		strFile.Format(_T("%d.%d.%d.%d"),
					   HIWORD(lpFfi->dwFileVersionMS), LOWORD(lpFfi->dwFileVersionMS),
					   HIWORD(lpFfi->dwFileVersionLS), LOWORD(lpFfi->dwFileVersionLS));
	}
	delete[] lpVersionInfo;

	return (0 < uLen);

ErrorReturn:
	GetSystemErrorMsg(GetLastError(), strProd);
	if (lpVersionInfo)
		delete[] lpVersionInfo;
	return false;
}


bool getAppVersions(LPCTSTR pszFilePath, stVersion& prodVer, stVersion& fileVer)
{
	DWORD dwHandle = 0;
	size_t buf_size = 0;
	UINT   uLen = 0;

	VS_FIXEDFILEINFO* lpFfi = NULL;
	LPBYTE lpVersionInfo = NULL;

	DWORD dwSize = GetFileVersionInfoSize(pszFilePath, &dwHandle);
	if (0 == dwSize)
		goto ErrorReturn;

	buf_size = ((dwSize+2)/2)*2;
	lpVersionInfo = new BYTE[buf_size];
	memset(lpVersionInfo, 0, buf_size);
	if (!GetFileVersionInfo(pszFilePath, 0, buf_size, lpVersionInfo))
		goto ErrorReturn;

	if (VerQueryValue(lpVersionInfo, _T("\\"), (LPVOID *)&lpFfi, &uLen) && 0 < uLen)
	{
		prodVer.ver[0] = BYTE(HIWORD(lpFfi->dwProductVersionMS));
		prodVer.ver[1] = BYTE(LOWORD(lpFfi->dwProductVersionMS));
		prodVer.ver[2] = BYTE(HIWORD(lpFfi->dwProductVersionLS));
		prodVer.ver[3] = BYTE(LOWORD(lpFfi->dwProductVersionLS));
		fileVer.ver[0] = BYTE(HIWORD(lpFfi->dwFileVersionMS));
		fileVer.ver[1] = BYTE(LOWORD(lpFfi->dwFileVersionMS));
		fileVer.ver[2] = BYTE(HIWORD(lpFfi->dwFileVersionLS));
		fileVer.ver[3] = BYTE(LOWORD(lpFfi->dwFileVersionLS));
	}
	delete[] lpVersionInfo;

	return (0 < uLen);

ErrorReturn:
	if (lpVersionInfo)
		delete[] lpVersionInfo;
	return false;
}