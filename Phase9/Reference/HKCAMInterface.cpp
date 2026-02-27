// HKCAMInterface.cpp : Defines the initialization routines for the DLL.
//

#include "stdafx.h"
#include "HKCAMInterface.h"
#include "afxpriv.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//
//TODO: If this DLL is dynamically linked against the MFC DLLs,
//		any functions exported from this DLL which call into
//		MFC must have the AFX_MANAGE_STATE macro added at the
//		very beginning of the function.
//
//		For example:
//
//		extern "C" BOOL PASCAL EXPORT ExportedFunction()
//		{
//			AFX_MANAGE_STATE(AfxGetStaticModuleState());
//			// normal function body here
//		}
//
//		It is very important that this macro appear in each
//		function, prior to any calls into MFC.  This means that
//		it must appear as the first statement within the 
//		function, even before any object variable declarations
//		as their constructors may generate calls into the MFC
//		DLL.
//
//		Please see MFC Technical Notes 33 and 58 for additional
//		details.
//

// CHKCAMInterfaceApp

BEGIN_MESSAGE_MAP(CHKCAMInterfaceApp, CWinApp)
END_MESSAGE_MAP()


// CHKCAMInterfaceApp construction

CHKCAMInterfaceApp::CHKCAMInterfaceApp()
{
	// TODO: add construction code here,
	// Place all significant initialization in InitInstance
}


// The one and only CHKCAMInterfaceApp object

CHKCAMInterfaceApp theApp;

// 파일명만 추출 하는 과정
inline void GetShortFileName(CString& strFilename)
{
	int iCut = strFilename.ReverseFind(_T('\\'));
	if (0 < iCut)
		strFilename = strFilename.Mid(iCut+1); // \\ 위치 이후의 문자열을 추출
	iCut = strFilename.ReverseFind(_T('.')); 
	if (0 < iCut)
		strFilename = strFilename.Left(iCut); // 확장자 .mpf의 . 위치 이전까지 최종 추출

	return;
}

// CHKCAMInterfaceApp initialization
// 모르것다...
BOOL CHKCAMInterfaceApp::InitInstance()
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	CWinApp::InitInstance();

	CString strAppName, strDllName;
	AfxGetModuleFileName(NULL, strAppName);
	GetShortFileName(strAppName);
	AfxGetModuleFileName(m_hInstance, strDllName);
	m_strDLLPath = strDllName;
	GetShortFileName(strDllName);

	CString strKey(strDllName+_T('@')+strAppName);
	SetRegistryKey(strKey);

	return TRUE;
}
