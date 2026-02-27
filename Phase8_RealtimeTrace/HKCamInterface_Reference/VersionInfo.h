#pragma once

bool getAppVersions(CString& strVerProd, CString& strFile);
bool getAppVersions(stVersion& verProd, stVersion& verFile);	// ProductVersion\nFileVersion; eg. 1.0.0.0\n1.1.1.1 => product version = 1.0.0.0 and file version = 1.1.1.1
bool getAppVersions(LPCTSTR pszFilePath, CString& strVerProd, CString& strFile);
bool getAppVersions(LPCTSTR pszFilePath, stVersion& verProd, stVersion& verFile);	// ProductVersion\nFileVersion; eg. 1.0.0.0\n1.1.1.1 => product version = 1.0.0.0 and file version = 1.1.1.1
