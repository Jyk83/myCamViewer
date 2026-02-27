#pragma once

#ifdef _DLL_EXPORT_
#define DLLSPEC	extern "C" __declspec(dllexport)
#else
#define DLLSPEC extern "C" __declspec(dllimport)
#endif // _DLL_EXPORT

///////////////////////////////////////////////////////////////////////////////////////////////
// Definitions of the interface parameters such as data types, return codes, and the like
///////////////////////////////////////////////////////////////////////////////////////////////

// +-----------------------------------------------------------------------
// | Callback, logging and status feedback interfaces
// +-----------------------------------------------------------------------

//
// Callback window messages
//
//---------------------------------------------------------------------------------------------------
// Message for notifying the current mouse position in workpiece coordinate systems
#define WNM_CAMVIEW_MOUSEMOVE		(WM_APP+201)	// WPARAM = CAM Viewer window handle, LPARAM = pointer to double[2] = x, y
//---------------------------------------------------------------------------------------------------
// Message for notice on changing view magnification scale
#define WNM_CAMVIEW_SCALECHANGED	(WM_APP+202)	//  WPARAM = CAM Viewer window handle, LPARAM  = point to double = scale

//
// Callback function prototype for tracing mouse pointers
typedef void(__stdcall *TraceMouseCoords)(double x, double y);
//
// Callback function prototype for logging event and/or error messages
typedef void(__stdcall *progressLog)(LPCTSTR pszMsg);
// Log level specification codes
#define LOGLEVEL_NONE		0
#define LOGLEVEL_INFO		1
#define LOGLEVEL_WARNING	2
#define LOGLEVEL_ERROR		3



// +-----------------------------------------------------------------------
// | CAM file (MPF file) information
// +-----------------------------------------------------------------------

const int c_camFilenameBufLen = MAX_PATH;
const int c_camDBnameBufLen = 128;
const int c_camMiscBufLen = 32;

struct CAMFILEINFO
{
	TCHAR sVersion[c_camMiscBufLen];	// MPF version string
	TCHAR sName[c_camFilenameBufLen];	// MPF filename
	TCHAR sPath[c_camFilenameBufLen];	// MPF file path including szFileName[]

	TCHAR sCDBname[c_camDBnameBufLen];	// Cutting DB filename
	TCHAR sMaterial[c_camMiscBufLen];	// Name of the workpiece material
	TCHAR sAssistGas[c_camMiscBufLen];	// Assist gas name

	double thickness;	// workpiece thickness in millimeters
	double width, height; // dimension of the workpiece in millimeters

	int number_of_part_types;		// the number of different shape types
	int number_of_parts;			// the number of parts
	int number_of_contours_total;	// the total number of contours
	double contour_length_total_mm;	// total length of contours
};


// +-----------------------------------------------------------------------
// | Possible return codes of the service functions
// +-----------------------------------------------------------------------

#define CVERR_UNKNOWN				-3	// can't trace back to the cause of error but happened anyway
#define CVERR_INVALID_ARGS			-2	// invalid input arguments were passed in
#define CVERR_OUTOFMEMORY			-1	// memory allocation error; hard to happen
#define CV_NOERROR					0	// success
#define CVERR_FILE_VERSION			1	// invalid or unsupported MPF version
#define CVERR_FILE_CAMFORMAT		2	// invalid format of MPF file
#define CVERR_WINDOW_NOTFOUND		101	// no target window, maybe by calling a window related function without creating a view window or data manipulation function without loading CAM data
#define CVERR_WINDOW_NOT_READY		102	// no target window, maybe by calling a window related function without creating a view window or data manipulation function without loading CAM data
#define CVERR_NOCONTENTS			103	// window created, but it has no content
#define CVERR_VIEWER_LOGFOLDER		111	// CVRestartSetLogFolder() is called with invalid folder information
#define CVERR_FILELOAD				201	// error happened while loading a MPF file. check the status of the file
#define CVERR_FILECREATE			202	// file creation error
#define CVERR_CAMDATA				301	// something wrong in the MPF data file data. either the file is corrupted or this program is out of date
#define CVERR_INVALID_OPTION		302	// CVSetOption() called with unrecognized option code. check the option codes below
#define CVERR_INVALID_OPT_VALUE		303	// CVSetOption() has an invalid option value. the only possible invalid value is the height of the part number. it should be greater than zero
#define CVERR_INVALID_PART_NO		304	// CVStartCutting() is called with invalid starting part number or/and contour number
#define CVERR_INVALID_CONTOUR_NO	305	//
#define CVERR_INVALID_BLOCK_INDEX	306	//
#define CVERR_INVALID_LINE_NO		307	// CVGetBlockString() is called with invalid line number
#define CVERR_BLOCK_BUFFER_SIZE		308	// CVGetBlockString() is called with a small buffer
#define CVERR_INVALID_OPTIMIAZTION	401	// CVCreateOptimalCAMFile() gets invalid optimization code
#define CVERR_FLYING_JUMPHEIGHTS	501	// Flying optimization is called with one or more invalid jump height values
#define CVERR_FLYING_NOTAPPLICABLE	502	// Flying optimization failed because of the input parameter settings
#define CVERR_FLYING_JERK_X			511	// Flying optimization input parameter errors : invalid jerk value of the x-axis
#define CVERR_FLYING_ACCL_X			512	//		invalid acceleration of the x-axis
#define CVERR_FLYING_FEED_X			513	//		invalid feed rate of the x-axis
#define CVERR_FLYING_JERK_Y			514	//		invalid jerk value of the y-axis
#define CVERR_FLYING_ACCL_Y			515	//		invalid acceleration of the y-axis
#define CVERR_FLYING_FEED_Y			516	//		invalid feed rate of the y-axis
#define CVERR_FLYING_JERK_Z			517	//		invalid jerk value of the z-axis
#define CVERR_FLYING_ACCL_Z			518	//		invalid acceleration of the z-axis
#define CVERR_FLYING_FEED_Z			519	//		invalid feed rate of the z-axis
#define CVERR_FLYING_DISTANCE		520	//		invalid distance is specified
#define CVERR_FLYING_TIME			521	//		invalid time limit is specified
#define CVERR_FLYING_FEEDS			522	//		invalid initial and/or final feed rates
#define CVERR_FLYING_NOT_READY		530	// Flying optimization system is not ready - internal error, most probably
#define CVERR_RESTART_FINISHED		600	// The previous work got already done, no more work to do
#define CVERR_RESTART_INTEGRITY		601	// Restart information is corrupted (for example, the line number is not consistent with the part number and/or the contour number)
#define CVERR_RESTART_STARTPOS		602	// can't determine the restart position
#define CVERR_RESTART_PART_NO		603	// invalid part number is specified. 1 <= part number <= number of nesting parts
#define CVERR_RESTART_PART_BLOCK_NO	604	// invalid command block number at the part shape information or contour
#define CVERR_RESTART_CONTOUR_NO	605	// invalid contour number if specified
#define CVERR_RESTART_LINE_NO		606	// invalid block line number
#define CVERR_RESTART_HKSTR			607	// can't find HKSTR for rewriting the restart file
#define CVERR_RESTART_NO_HKEND		608	// HKEND not found
#define CVERR_RESTART_M30NOTFOUND	609	// Nesting end M function not found
#define CVERR_RESTART_HKOST			610	// HKOST not found or HKOST has invalid arguments
#define CVERR_RESTART_HKSUB			611	// invalid HK*.SPF found
#define CVERR_RESTART_GCODE			612	// invalid G-code
#define CVERR_HSCRC_UNSUPPORTED		613	// Remnant cutting is not supported in 'Restart-work' yet
#define CVERR_RESTART_LOGFOLDER		620	// CVRestartSetLogFolder() is called with invalid folder information
#define CVERR_RESTART_INTERNAL		699	// internal error that can't happen in a normal case




// +-----------------------------------------------------------------------
// | CAM parts drawing Option codes
// +-----------------------------------------------------------------------

#define iOPT_DRAW_PIERCING			101		// nValue==0 => do not draw, otherwise draw them
#define iOPT_PART_NUMBER			102		// nValue==0 => do not display, otherwise display the numbers
#define iOPT_TRACE_CUTTINGSPOT		103		// nVlaue==0 => leave the screen as it is, otherwise make sure the current cutting spot always visible
#define iOPT_PARTNO_HEIGHT			104		// nValue=height of the part number in pixels
#define iOPT_CONTOUR_NUMBER			105		// nValue==0 => do not display, otherwise display the numbers
#define iOPT_CONTOURNO_HEIGHT		106		// nValue = height of the contour number in pixels
#define iOPT_LIMIT_PART_CONT_NO		107		// nValue=the number of (parts + contours) to which the viewer is allowed to display numbers
#define iOPT_COLOR_BACKGROUND		201		// nValue=color of the window background, use the code snippet 'MakeRGBColor()' below to make a desirable colors of yours
#define iOPT_COLOR_WORKPIECE		202		// nValue=color of the workpiece background, this appears in 50% transparent color on the window
#define iOPT_COLOR_PART_NUMBER		203		// nValue=color of the part numbers
#define iOPT_COLOR_CONTOUR_NUMBER	204		// nValue=color of the contour numbers
#define iOPT_COLOR_PIERCING			205		// nValue=color of the piercing spot
#define iOPT_COLOR_LEADIN			206		// nValue=color of the lead-in contour
#define iOPT_COLOR_CONTOUR			207		// nValue=color of the cutting path on the workpieces
#define iOPT_COLOR_MARKING			208		// nValue=color of the marking(engraving) contours
#define iOPT_COLOR_CUTTING			209		// nValue=color of the cutting progress paths
#define OPT_NUM_COLORS				9

enum DrawingColor
{
	enColorBackground = 0,
	enColorCanvas,
	enColorPartNo,
	enColorContourNo,
	enColorPiercing,
	enColorLeadIn,
	enColorContour,
	enColorEngraving,
	enColorCutProgress,
};
const int c_last_color = enColorCutProgress;

struct DrawingOption
{
	COLORREF color[OPT_NUM_COLORS];
	bool show_part_number;
	bool show_cont_number;
	bool show_piercing;
	bool show_ensure_visible;
	int  pixelheight_part_number;
	int  pixelheight_cont_number;
};


// +-----------------------------------------------------------------------
// | CAM motion profile data
// +-----------------------------------------------------------------------

// Profile optimization codes
#define iOPTIMIZATION_NONE	0
#define iOPTIMIZATION_JUMP	0x01	// 00000000 00000000 00000000 00000001
#define iOPTIMIZATION_RPP	0x02	// 00000000 00000000 00000000 00000010

// Basic motion parameters
struct MotionSpec
{
	int jerk;	// m/s^3, the limited jerk of machine data for an axis
	int accel;	// m/s^2, the allowed maximum acceleration of machine data for an axis
	int speed;	// mm/min, the allowed maximum speed of machine data for an axis

	double jerkLimitScale;	// practical jerk limit = jerk*jerkLimitScale
	double acclLimitScale;	// practical acceleration limit = accel*acclLimitScale
};

// MPF optimization parameters

// Measurement unit system
#define MEASURES_NONE		0
#define MEASURES_METRIC		1
#define MEASURES_INCHES		2

// Flying mode jump optimization parameter for iOPTIMIZATION_JUMP code
struct JumpParam
{
	// Motion specifiers
	MotionSpec	X, Y, Z;	// predefined motion settings for each motor axis

	// motion specification along the z-axis
	int measureSystem;		// either of metric (mm = 1) or inches(" = 2)

	// part-to-part jump conditions
	bool useMPFSettings;
	// 1. Before jump (cutting or marking)
	double zCuttingGapNormal;		// Z axis height for CW cutting; used when true == useMPFSettings
	double zCuttingGapPulse;		// Z axis height for pulse cutting; used when true == useMPFSettings
	double zCuttingGapHMISetting;	// Z axis height for cutting defined on HMI; used when false == useMPFSettings
	// 2. At jump, the maximum height of jump
	double zJumpHeight;			// jump height in mm
	// 3. Landing height after jump
	double zPiercingGap;		// Z axis height of jump destination in mm (for piercing-cutting)
	// 4. Special condition for before and after jump, the marking height
	double zMarkingGap;			// Z axis height of jump destination in mm (for marking)
	double zShotMarkingGap;		// Z axis height of jump destination in mm (for marking)

	// Misc. options
	double minJumpRadius;	// 0=> always try flying mode, otherwise jump only if the distance to the next contour is greater than this 'minJumpRadius'.
	double maxJumpRadius;	// 0=> always try flying mode, otherwise flying jump only if the distance to the next contour is less than this 'maxJumpRadius'.
	int    jumpSyncDelay;	// wait time in milliseconds for synchronizing horizontal motion with jump up-and-down motion
							// the reason for this option is because jump up-and-down motion actually consists of 2 commands
};


// +-----------------------------------------------------------------------
// | Restart work parameter
// +-----------------------------------------------------------------------

const int programNameBufLen = 64;
const int blockStringBufLen = 128;

struct RestartInfo
{
	// Log information of the last block
	int		partNo;		// the part number where the last work stopped
	int		contourNo;	// the contour number in the partNo where the last work stopped
	int		lineNo;		// the line number of part program where the last block is
	double	stopXwcs;	// x-coordinate of the stop position in Workpiece Coordinate System
	double	stopYwcs;	// y-coordinate of the stop position in Workpiece Coordinate System

	// Restart option
	double startingOffset;	// restart position relative to the 'cutLengthSoFar'
		// 0 > startingOffset => move back by '|startingOffset|' and resume cutting there
		// 0 = startingOffset => restart from the last position, (stopXwcs, stopYwcs)
		// 0 < startingOffset => skip by 'startingOffset' from (stopXwcs, stopYwcs) and resume
	bool isReverseOrder;// true => the part cutting order was in reverse (the last one => the first)
};


// +-----------------------------------------------------------------------
// | Cutting progress information
// +-----------------------------------------------------------------------

struct CutProgress
{
	int startPart;
	int startContour;
	int endPart;
	int endContour;
	int endElement;
	double endProgress;
};


///////////////////////////////////////////////////////////////////////////////////////////////
// Prototypes of the service functions
///////////////////////////////////////////////////////////////////////////////////////////////

// +-----------------------------------------------------------------------
// | Viewer window handling such as creating, destroying, moving, zooming its contents
// +-----------------------------------------------------------------------
DLLSPEC HWND CVCreateWindow(// creates CAM file viewer window, returns the handle of window if successful or NULL otherwise
							HWND hWndParent,	// handle of parent window
							UINT ID,			// this (child) window's ID (arbitrary id for the parent window)
							int x, int y,		// upper left corner position in the client area of the parent window
							int cx, int cy,		// dimension of window in pixels
							TraceMouseCoords pfn	//address of call back for trace mouse coordinates
							);

DLLSPEC int CVDestroyWindow(// destroys a CAM file viewer window
							HWND hWndCV			// handle of the CAM file viewer window to be destroyed
							);

DLLSPEC int CVRelocateWindow(// resize and/or move CAM file viewer window within the parent window's client area
							 HWND hWndCV,		// handle of CAM file viewer window
							 int x, int y,		// upper left corner position in the client area of the parent window
							 int cx, int cy		// dimension of window in pixels
							 );

DLLSPEC int CVZoomContens(	// scales image view
						  HWND hWndCV,		// handle of CAM file viewer window
						  BOOL bScaleUp		// TRUE -> enlarge, FALSE -> shrink down
						  );

DLLSPEC int CVZoomFitToWindow(	// resize the content to make a full view of it
							  HWND hWndCV			// handle of CAM file viewer window
							  );

DLLSPEC int CVZoomActualSize(	// resizes the content to its real view size
							 HWND hWndCV
							 );

DLLSPEC double CVGetZoomScale(	// gets the current zoom scale
							  HWND hWndCV
							  );

DLLSPEC int CVSetViewLogFolder( // set log folder
							   LPCTSTR pszFolder);	// NULL => don't create log file
													// otherwise, create log files under the specified folder

// +-----------------------------------------------------------------------
// | Data handling such as loading data files, setting data display options
// +-----------------------------------------------------------------------
DLLSPEC int CVLoadCAMFile(	// loads a CAM file
						  HWND hWndCV,		// handle of the CAM file viewer that will own the content of the file
						  LPCTSTR pszFilePath,	// path name for the file to be loaded
						  char* pszError,
						  int nMaxBuffer
						  );

DLLSPEC int CVDeleteContents(HWND hWndCV);	// clears the existing CAM drawing contents and empties the screen

DLLSPEC int CVSetOption(	// sets various display options of the viewer window
						HWND hWndCV,
						int iOption,		// see the option code definition below, iOPT_*
						unsigned int nValue	// depending on the options; see the comment corresponding each option below
						);
DLLSPEC int CVGetOption(	// gets various display options of the viewer window
						HWND hWndCV,
						int iOption,		// see the option code definition below, iOPT_*
						unsigned int& nValue
						);
DLLSPEC int CVGetOptions(
						HWND hWndCV,
						DrawingOption& opt
						);

DLLSPEC int CVGetWholeSize(	//gets the whole size of the sheet
					   HWND hWndCV,
					   double& width,
					   double& height
					   );

DLLSPEC void CVSetProgressMonitor(progressLog pfn, int nLogLevel);

// +-----------------------------------------------------------------------
// | Cutting progress monitoring / simulation
// +-----------------------------------------------------------------------
DLLSPEC int CVStartCutting(		// starts cutting progress monitoring on the screen
						   HWND hWndCV,
						   int nPartFrom,		// starting part number
						   int nContourFrom,	// starting contour number of the starting part
						   int nIsReverse		// 0==normal forward cutting, otherwise reversing backward cutting
						   );

DLLSPEC int CVUpdateCutting(	// updates the current cutting progress on the screen
							HWND hWndCV,			// handle of CAM file viewer window
							int part,				// the current part number under progress
							int contour,			// the current contour number under progress
							const char* currentBlock,
							double progress,
							double xwcs,
							double ywcs,
							int isGcodeBlock	//Whether the current block is G-code or not
							);

DLLSPEC int CVUpdateCutting2(	// updates the current cutting progress on the screen
							HWND hWndCV,			// handle of CAM file viewer window
							int part,				// the current part number under progress
							int contour,			// the current contour number under progress
							int mpfLineNo,			// line number of the current block in MPF
							double progress,		// progress of cutting in the current block
							double xwcs,			// x and y position of the current spot in the current block
							double ywcs				// in workpiece coordinates
							);

DLLSPEC int CVFinishCutting(	// lets the viewer know the cutting is completely done
							HWND hWndCV
							);

DLLSPEC int CVCompleteLastElement(	// lets the viewer know the last element cutting is completely done
								  HWND hWndCV
								  );

DLLSPEC int CVCompleteLastContour(	// lets the viewer know the last contour cutting is completely done
								  HWND hWndCV
								  );

DLLSPEC int CVStopCutting(HWND hWndCV);	// stops the cutting monitoring.
										// the viewer screen will keep the progress monitoring result
										// until the next call of CVStartCutting(), or CVResetCuttingProgress().
DLLSPEC int CVResetCutting(HWND hWndCV);	// clears the previous cutting progress monitoring result on the screen

DLLSPEC int CVSetCuttingDone(HWND hWndCV, //Sets the designated areas as the complete areas for simulation
							int fromPart,
							int fromContour,
							int toPart,
							int toContour,
							int isReverse);

DLLSPEC int CVSetCuttingProgressDone(HWND hWndCV, //Sets the designated areas as the complete areas for simulation
							int fromPart,
							int fromContour,
							int toPart,
							int toContour,
							int toElement,
							double toProgress);


// +-----------------------------------------------------------------------
// | CAM data information
// +-----------------------------------------------------------------------

DLLSPEC int CVGetCAMFileInfo(	// retrieves information about the CAM file just loaded
							 HWND hWndCV,	// handle of the CAM file viewer that will own the content of the file
							 CAMFILEINFO* pInfo	// data structure of file information
							 );
DLLSPEC int CVGetPartCount(HWND hWndCV, int& partCount);
DLLSPEC int CVGetContourCount(HWND hWndCV, int part, int& contourCount);
DLLSPEC int CVGetContourLength(HWND hWndCV, int part, int contour, double& length, bool isReset);	//returns the entire length of a contour
DLLSPEC int CVGetElementCount(HWND hWndCV, int part, int contour, int& elementCount);
DLLSPEC int CVGetElementInfo(HWND hWndCV, int part, int contour, int element, int& lineNo, double& length);
DLLSPEC int CVGetElementPos(HWND hWndCV, int part, int contour, int element, double progress, double& wcsX, double& wcsY);
DLLSPEC int CVGetBlockProgress(HWND hWndCV, int partNo, int contourNo, int lineNo, double wcsX, double wcsY, double& cutDone, double& cutRemains);
DLLSPEC int CVGetElementBlockCode(HWND hWndCV, int part, int contour, int element, char* pszBlockCode, int maxbuffer);	//returns the block code of a element
DLLSPEC int CVGetScanCut(HWND hWndCV, int& scancut);	// retrieves information whether Scan cut is included or not.
DLLSPEC int CVGetBlockString(HWND hWndCV, int lineNo, PTSTR pszBlock, int bufferLength);
DLLSPEC int CVGetMPFContent(HWND hWndCV, CStringArray& arrLine);
DLLSPEC void CVClearMPFContent(CStringArray& arrLine);


// +-----------------------------------------------------------------------
// | CAM optimization to create a new MPF file
// +-----------------------------------------------------------------------

DLLSPEC int CVCreateOptimalCAMFile(	// writes optimized version of MFP file
								   HWND hWndCV,				// handle of the CAM file viewer having CAM data to write
								   int nOptimizationCode,	// bitwise combination of selected optimizations
								   LPVOID pParam,			// input parameter for optimization
								   LPCTSTR pszFile);		// target path (in full path)


DLLSPEC int CVCreateOptimalCAMFileWithOrgFile(	// writes optimized version of MFP file
									int nOptimizationCode,	// bitwise combination of selected optimizations
									LPVOID pParam,			// input parameter for optimization
									LPCTSTR pszOrgFile,		// source file path
									LPCTSTR pszDestFile,
									char* pszError,			//Error message if it returns an error
									int maxErrorBuffer);			//Max buffer of error message

// +-----------------------------------------------------------------------
// | CAM motion profiling for optimization
// +-----------------------------------------------------------------------

DLLSPEC int CVTraceFlyingModeJump(	// writes jump optimization trace file
								  HWND hWndCV,		// handle of the CAM file viewer having CAM data to write
								  LPVOID pParam,	// JumpParam parameter
								  LPCTSTR pszFile);	// trace file path


// +-----------------------------------------------------------------------
// | Restart-Work MPF file generation
// +-----------------------------------------------------------------------
DLLSPEC int CVCreateRestartCAMFile(	// creates Restart-work MPF file from the original file with the specified Restart-work option
								   RestartInfo& option,		// Restart-work option
								   LPCTSTR pszCAMFile,		// the original MPF file
								   LPCTSTR pszRestartFile);	// new MPF file for 'Restart-work'
DLLSPEC int CVRestartSetLogFolder( // set log folder
								  LPCTSTR pszFolder);		// NULL => don't create log file
															// otherwise, create log files under the specified folder

// +-----------------------------------------------------------------------
// | Misc. option
// +-----------------------------------------------------------------------

DLLSPEC void CVTurnMessageON();
DLLSPEC void CVTurnMessageOFF();



// +-----------------------------------------------------------------------
// | Version information
// +-----------------------------------------------------------------------

struct stVersion { BYTE ver[4]; };
				// 1.2.3.4 => ver[0]=1, ver[1]=2, ver[2]=3, ver[3]=4
const int c_verInfoStrBufLen = 16;
				// max version information = XXX.XXX.XXX.XXX
DLLSPEC bool CVGetVersionVals(stVersion& prodVer, stVersion& fileVer);
DLLSPEC bool CVGetVersionStrs(TCHAR szProd[c_verInfoStrBufLen], TCHAR szFile[c_verInfoStrBufLen]);
DLLSPEC void CVGetBuildInfo(TCHAR szBuild[c_verInfoStrBufLen]);




// +-----------------------------------------------------------------------
// | Cutting progress information
// +-----------------------------------------------------------------------

DLLSPEC int CVGetCuttingProgressInfo(	// returns cutting progress information
							HWND hWndCV,	
							LPARAM& pInfo,
							int& nCount);

DLLSPEC int CVGetCurrCutDistance(	// returns the cut distance
							HWND hWndCV,
							double& cutDistance);
