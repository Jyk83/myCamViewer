#pragma once

#include "HKCAMInterfaceDLL.h"
#include "Graph2d.h"


//////////////////////////////////////////////////////////////////////////

#define PROFILE_SUCCESS				 0
#define PROFILE_OK					 0
#define PROFILE_ERR_MEMORY			-1
#define PROFILE_INVALID_INPUT		 1
#define PROFILE_ERR_NOT_FLYINGMODE	 2

// Error codes for details
#define PROFILE_ERR_INPUT_JERK_X	11
#define PROFILE_ERR_INPUT_ACCL_X	12
#define PROFILE_ERR_INPUT_FEED_X	13
#define PROFILE_ERR_INPUT_JERK_Y	14
#define PROFILE_ERR_INPUT_ACCL_Y	15
#define PROFILE_ERR_INPUT_FEED_Y	16
#define PROFILE_ERR_INPUT_JERK_Z	17
#define PROFILE_ERR_INPUT_ACCL_Z	18
#define PROFILE_ERR_INPUT_FEED_Z	19
#define PROFILE_ERR_INPUT_DISTANCE	20
#define PROFILE_ERR_INPUT_TIME		21
#define PROFILE_ERR_INPUT_FEEDS		22

#define PROFILE_ERR_NOT_INITIALIZED	100

#define nJUMPTYPE_NONE				0
#define nJUMPTYPE_UPDOWN			1
#define nJUMPTYPE_HALFFLYING		2
#define nJUMPTYPE_FLYING			3

//////////////////////////////////////////////////////////////////////////

// +--------------------------------------------------------------------+
// | Motor settings                                                     |
// +--------------------------------------------------------------------+

struct MotionDef	// in metric
{
	double jerk;	// mm/sec^3
	double accl;	// mm/sec^2
	double feed;	// mm/sec
};


// +--------------------------------------------------------------------+
// | Flying jump information                                            |
// +--------------------------------------------------------------------+

enum JumpSpot
{
	spotUndefined,
	spotCutting,
	spotMarking,
	spotShotMarking,
	spotPiercing,
};

struct JumpInfo
{
	// Type jumps
	JumpSpot spotFrom;
	JumpSpot spotTo;

	// Motion vector of flying jump in the x-y plane
	double xyDistance;
	double xyAngleRad;
};

struct FlyingParam
{
	// Performance of the motor axes
	MotionDef motorX, motorY, motorZ;

	// Jump information
	JumpInfo jump;

	// Various cutting clearance options
	double zCuttingGapCW;	// clearance at CW cutting (height just before jumping off)
	double zCuttingGapPulse;
	double zCuttingHMISet;
	double zMarkingGap;		// clearance of marking position
	double zShotMarkingGap;	// clearance of shot-marking position
	double zPiercingGap;

	// Clearances between workpiece and the cutting head tip in mm
	double zJumpStart;	// clearance just before jumping off
	double zJumpHeight;	// distance between workpiece and the peak of jumps
	double zJumpTarget;	// clearance at piercing position (usually the height of jump destination of cutting process)

	// Misc. options
	double minJumpRadius;	// 0=> always jump up, otherwise jump only if the distance to the next contour is greater than this 'minJumpRadius'.
	double maxJumpRadius;	// 0=> always jump up, otherwise jump only if the distance to the next contour is less than this 'maxJumpRadius'.
	int    jumpSyncDelay;	// wait time in milliseconds for synchronizing horizontal motion with jump up-and-down motion
							// the reason for this option is because jump up-and-down motion actually consists of 2 commands

	FlyingParam() { memset(this, 0, sizeof(FlyingParam)); }
};


// +--------------------------------------------------------------------+
// | Class for Single-Line Velocity profile,                            |
// |                  symmetrical with zero initial & final velocities  |
// +--------------------------------------------------------------------+

class SLProfile
{
	// Constructor(s) and destructor
public:
	SLProfile();
	~SLProfile();

	// Operations
public:
	// Profile generations according to the given conditions
	int generateXY(const MotionDef& axisX, const MotionDef& axisY, double angle_rad, double distance);
	int generateZ(const MotionDef& axis, double distance);

	void clear();

	// Attributes
public:
	bool hasGenerated() const;

	double timeAccel() const { return (Tj+Ta+Tj); }
	double timeTotal() const { return (2*timeAccel() + Tv); }
	double distTotal() const { return (2*Fmax*Tj + dSv() + 2*dSa()); }

	bool residualForTime(double time, double& distance) const;

	double jerk() const { return Jerk; }
	double accelMax() const { return Amax; }
	double feedMax() const { return Fmax; }

	double periodJerkApply() const { return Tj; }
	double periodConstAccl() const { return Ta; }
	double periodConstFeed() const { return Tv; }

	// dVj() = velocity change during jerk application time
	// dVa() = velocity change during constant acceleration time
	// dSj() = distance traveled during initial Tj
	// dSa() = distance traveled during the constant acceleration time
	// dSv() = distance traveled during the constant maximum feedrate time
	double dVj() const { return (0.5*Jerk*Tj*Tj); }
	double dVa() const { return (Amax*Ta); }
	double dSj() const { return ((1.0/6.0)*Jerk*pow(Tj, 3.0)); }
	double dSa() const { return (0.5*Fmax*Ta); }
	double dSv() const { return (Fmax*Tv); }

	// Implementation
protected:
	// Motion parameters
	double Jerk;	// mm/sec^3, always positive single value; the limited constant jerk
	double Amax;	// mm/sec^2, absolute value; the maximum acceleration
	double Fmax;	// mm/sec, absolute value; the maximum feedrate

	// Profile feature values
	double Tj;	// sec; jerk time - the period where the jerk is applied
	double Ta;	// sec; constant accleration time
	double Tv;	// sec; constant feedrate time

	int init();	// generate a default profile based on the given limited jerk, allowable maximum acceleration, and allowable maximum feedrate
	int fitToDist(double distance);	// generate the profile satisfying the given distance condition
	int fitToTime(double time);	// fix the current profile to fit it into a fixed time interval cutting off the corresponding travel distance
};

inline SLProfile::SLProfile() { memset(this, 0, sizeof(SLProfile)); }
inline SLProfile::~SLProfile() { }
inline bool SLProfile::hasGenerated() const { return (0 < Jerk && IsSame(Amax, Jerk*Tj) && IsSame(Fmax, dVj()+dVa()+dVj())); }
inline void SLProfile::clear() { memset(this, 0, sizeof(SLProfile)); }


// +--------------------------------------------------------------------+
// | Class for Continuous-Line Velocity profile,                        |
// |                         with specified initial & final velocities  |
// +--------------------------------------------------------------------+

struct CLParam
{
	MotionDef axisX, axisY;			// motor axis info
	double	xyAngleRad, xyDistance;	// motion vector description in radian, mm respectively
	double	initialFeed, finalFeed;	// initial & final velocities in mm/sec
};

class CLProfile
{
	// Constructor(s) and destructor
public:
	CLProfile();
	~CLProfile();

	// Operations
public:
	// Profile generations according to the given conditions
	int generate(const CLParam param);

	void clear();

	// Attributes
public:
	bool hasInitialized() const;
	bool hasGenerated() const;

	double jerk() const { return Jerk; }
	double initialFeedRate() const { return F_i; }
	double finalFeedRate() const { return F_f; }
	double limitFeedRate() const { return Fmax; }
	double feedPossible() const { return V_max; }
	double accelMax() const { return Amax1; }
	double decelMax() const { return Amax2; }
	double accelJerkTime() const { return Tj1; }
	double decelJerkTime() const { return Tj2; }
	double accelConstTime() const { return Ta1; }
	double decelConstTime() const { return Ta2; }
	double constFeedTime() const { return Tv; }

	double timeAccel() const { return (Tj1+Ta1+Tj1); }
	double timeDecel() const { return (Tj2+Ta2+Tj2); }
	double timeTotal() const { return (timeAccel() + Tv + timeDecel()); }
	double distTotal() const { return (dSa1() + dSv() + dSa2()); }

	// dVj1,2() = velocity change during jerk application time
	// dVa1,2() = velocity change during constant acceleration time
	// dSa1,2() = distance traveled during the constant acceleration time
	// dSv() = distance traveled during the constant maximum feedrate time
	double dVj1() const { return (0.5*Jerk*Tj1*Tj1); }
	double dVj2() const { return (0.5*Jerk*Tj2*Tj2); }
	double dVa1() const { return (Amax1*Ta1); }
	double dVa2() const { return (Amax2*Ta2); }
	double dSa1() const { return (F_i*(Tj1+Ta1) + 0.5*(dVj1()+dVa1())*Ta1 + V_max*Tj1); }
	double dSa2() const { return (F_f*(Tj2+Ta2) + 0.5*(dVj2()+dVa2())*Ta2 + V_max*Tj2); }
	double dSv() const { return (V_max*Tv); }

	// Implementation
protected:
	// Motion parameters
	double Jerk;	// mm/sec^3, always positive single value; the limited constant jerk
	double Amax;	//  mm/sec^2, absolute value; the maximum acceleration of motion system
	double Fmax;	// mm/sec, absolute value; the maximum feed rate
	double F_i;		// mm/sec, initial feed rate given
	double F_f;		// mm/sec, final feed rate specified

	// Profile feature values
	double Tj1, Tj2;// sec; jerk time - the period where the jerk is applied
	double Amax1;	// mm/sec^2, absolute value; the maximum acceleration when speeding up
	double Amax2;	// mm/sec^2, absolute value; the maximum acceleration when speeding down
	double Ta1, Ta2;// sec; constant accleration time
	double V_max;	// mm/sec, the possible maximum feed rate of this profile
	double Tv;		// sec; constant feedrate time

	bool init();	// generate a default profile based on the given limited jerk, allowable maximum acceleration, and allowable maximum feed rate
	int  fitToDistance(double distance);		// generate the profile satisfying the given distance condition
	void adjustMaxFeed(const double distance);	// adjust maximum feed rate to satisfy the given travel distance
	double getDistanceFor(double maxFeed, double iniFeed, double& Tj_, double& Accel_, double& Ta_);
};

inline bool CLProfile::hasInitialized() const
{
	return (0 < Jerk && 0 < Fmax && 0 < Amax);
}

inline bool CLProfile::hasGenerated() const
{
	return (hasInitialized() && 0 < V_max
	&& IsSame(V_max, F_i+2*dVj1()+dVa1()) && IsSame(V_max, F_f + 2*dVj2()+dVa2()));
}

inline CLProfile::~CLProfile() {}


// +--------------------------------------------------------------------+
// | Jump motion profiling and tracing                                  |
// +--------------------------------------------------------------------+

struct Trace
{
	UINT	time_ms;
	double	jerk, accel;	// motor status
	double	feed, distance;	// trajectory status
};

struct TraceBuf
{
	size_t trace_count;
	Trace* p;

	TraceBuf();
	~TraceBuf();
	void clear();
	bool alloc(double period, UINT sampling_interval_ms);
};

struct FlyingTrace
{
	// input
	UINT ms_sampling;	// sampling interval

	// output
	size_t trace_count;	// number of trace data
	Trace* pXY;			// trace data on the X-Y plane
	Trace* pZ;			// trace data along the Z-axis

	FlyingTrace() { memset(this, 0, sizeof(FlyingTrace)); }
	~FlyingTrace() { clear(); }

	void clear();
};


class FlyingProfile
{
	// Constructor(s) and destructor
public:
	FlyingProfile();
	~FlyingProfile();

	// Operation
public:
	// Motion specifications setting and profile generation
	int generate(const FlyingParam& param);
	// Result report
	int jumpInfo(double& ascendingDist, double& residualForDescending, double& descendingDist) const;
	int getJumpType() const;
	bool trace(FlyingTrace& buf) const;

	// Attributes
public:
	const SLProfile& traversingProfile() const;
	const SLProfile& jumpUpProfile() const;
	const SLProfile& landingProfile() const;

	double	jumpStartHeight() const;
	double	distToAscend() const;
	double	distToDescend() const;
	double	landingHeight() const;
	int		jumpUpDirection() const;
	int		landingDirection() const;

	double jumpSyncWaitTime() const;

	// Implementation
protected:
	// Motion profiles
	SLProfile xyProfile;		// [in] horizontal motion profile
	SLProfile zUpProfile;		// [in] vertical ascending motion profile
	SLProfile zDownProfile;		// [in] vertical descending motion profile
	double upDownSyncWaitTime;	// [in] jump synchronization of descending with traversing end

	// Jump start-end spot gaps
	double zJumpStart, zJumpEnd;

	// The final resultant jump information
	double heightToAscend;
	double distanceToDescend;
	double residualForStartDescending;

	int resultantJumpType;

	enum flyingCond
	{
		invalidInput = -1,
		conditionOk = 0,
		notFlyingMode,
		flyingNotApplicable,
	};

	void clear();

	flyingCond checkFlyingCondition(const FlyingParam& param);
	int residualOnHalfOrNoJump(const MotionDef& Z, const double timeTotal);
	int adjustJumpUpDownProfile(const MotionDef& axisZ, const double timeTotal, double t_sum);
	int finishJumpHeightBiSect(const MotionDef& Z, const double timeTotal, double curr_zHeight);

	bool isValid() const;
	bool canTrace() const;
};

inline void FlyingProfile::clear()
{
	xyProfile.clear(), zUpProfile.clear(), zDownProfile.clear();
	upDownSyncWaitTime = 0;
	zJumpStart = zJumpEnd = 0;
	heightToAscend = distanceToDescend = residualForStartDescending = 0;
	resultantJumpType = nJUMPTYPE_NONE;
}

inline bool FlyingProfile::isValid() const
{
	return (xyProfile.hasGenerated() && zUpProfile.hasGenerated() && zDownProfile.hasGenerated()
			&& GE(xyProfile.timeTotal(), zUpProfile.timeTotal()+zDownProfile.timeTotal()));
}
inline bool FlyingProfile::canTrace() const
{
	return (xyProfile.hasGenerated() && (zUpProfile.hasGenerated() || zDownProfile.hasGenerated())
			&& GE(xyProfile.timeTotal(), zUpProfile.timeTotal()+zDownProfile.timeTotal()));
}

inline const SLProfile& FlyingProfile::traversingProfile() const { return xyProfile; }
inline const SLProfile& FlyingProfile::jumpUpProfile() const { return zUpProfile; }
inline const SLProfile& FlyingProfile::landingProfile() const { return zDownProfile; }
inline double FlyingProfile::jumpStartHeight() const { return zJumpStart; }
inline double FlyingProfile::landingHeight() const { return zJumpEnd; }
inline int FlyingProfile::jumpUpDirection() const { return (0 > heightToAscend)? -1: 1; }
inline int FlyingProfile::landingDirection() const { return (0 > distanceToDescend)? -1: 1; }
inline double FlyingProfile::distToAscend() const { return heightToAscend; }
inline double FlyingProfile::distToDescend() const { return distanceToDescend; }
inline double FlyingProfile::jumpSyncWaitTime() const { return upDownSyncWaitTime; }
inline int FlyingProfile::getJumpType() const { return resultantJumpType; }

// +--------------------------------------------------------------------+
// | Flying profile data                                                |
// +--------------------------------------------------------------------+

#define NUM_PHASES		7
#define PHASE_I			0
#define PHASE_II		1
#define PHASE_III		2
#define PHASE_IV		3
#define PHASE_V			4
#define PHASE_VI		5
#define PHASE_VII		6
#define PHASE_JERK1		(PHASE_I)
#define PHASE_ACCEL		(PHASE_II)
#define PHASE_JERK2		(PHASE_III)
#define PHASE_CFEED		(PHASE_IV)
#define PHASE_JERK3		(PHASE_V)
#define PHASE_DECEL		(PHASE_VI)
#define PHASE_JERK4		(PHASE_VII)

enum traceStat
{
	stat_Ready,
	stat_jerkAccelUp,
	stat_constAccelUp,
	stat_jerkDecelUp,
	stat_constFeedrate,
	stat_jerkDecelDown,
	stat_constAccelDown,
	stat_jerkAccelDown,
	stat_Finished,
};

struct ProfileData
{
	double _Time[NUM_PHASES];
	double _Dist[NUM_PHASES];

	double jerk, a_max, v_max, _dVj;

	ProfileData() { memset(this, 0, sizeof(ProfileData)); }
	bool isValid() const;
	bool trace(const SLProfile& profile, UINT sampling_ms, TraceBuf& data);
	bool trace(const FlyingProfile& profile, UINT sampling_ms, TraceBuf& data);

protected:
	bool init(const SLProfile& profile);
	bool trace(traceStat& st, double t, Trace& result);
};

inline bool ProfileData::isValid() const
{
	return (0 < jerk && 0 < _Time[PHASE_JERK1] && 0 < _dVj && IsSame(a_max, jerk*_Time[PHASE_JERK1]));
}

//////////////////////////////////////////////////////////////////////////

