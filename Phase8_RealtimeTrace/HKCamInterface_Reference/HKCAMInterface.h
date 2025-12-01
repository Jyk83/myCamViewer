// HKCAMInterface.h : main header file for the HKCAMInterface DLL
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// CHKCAMInterfaceApp
// See HKCAMInterface.cpp for the implementation of this class
//

class CHKCAMInterfaceApp : public CWinApp
{
public:
	CHKCAMInterfaceApp();

	LPCTSTR GetDLLFilePath() const { return m_strDLLPath; }

// Overrides
public:
	virtual BOOL InitInstance();
	CString m_strDLLPath;

	DECLARE_MESSAGE_MAP()
};

extern CHKCAMInterfaceApp theApp;
