#include "stdafx.h"
#include "MotionPlanning.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif // _DEBUG


//////////////////////////////////////////////////////////////////////////

#define PROFILE_ERR_INPUT_JERK	PROFILE_ERR_INPUT_JERK_X
#define PROFILE_ERR_INPUT_ACCL	PROFILE_ERR_INPUT_ACCL_X
#define PROFILE_ERR_INPUT_FEED	PROFILE_ERR_INPUT_FEED_X

inline int checkMotionDef(const MotionDef& axis)
{
	if (0 >= axis.jerk)
		return PROFILE_ERR_INPUT_JERK;
	if (0 >= axis.accl)
		return PROFILE_ERR_INPUT_ACCL;
	if (0 >= axis.feed)
		return PROFILE_ERR_INPUT_FEED;
	return PROFILE_OK;
}

inline void getElements(double& v_max, double cos¥è, double sin¥è, double& vx, double&vy)
{
	ASSERT(GE(Abs(cos¥è), 0) && LE(Abs(cos¥è), 1.0)
		   && GE(Abs(sin¥è), 0) && LE(Abs(sin¥è), 1.0)
		   && IsSame(1.0, cos¥è*cos¥è+sin¥è*sin¥è));

	if (LE(cos¥è, 0) && LE(sin¥è, 0))
	{
		ASSERT(FALSE);
		vx = vy = 0;
		return;
	}

	double rx_max = vx/cos¥è, ry_max = vy/sin¥è;
	if (GE(rx_max, v_max) && GE(ry_max, v_max))
	{
		vx = v_max*cos¥è;
		vy = v_max*sin¥è;
	}
	else
	{
		const double vx_o = vx;
		const double vy_o = vy;
		if (LT(rx_max, ry_max))
		{
			// vx = vx;
			v_max = rx_max;
			vy = v_max*sin¥è;
			ASSERT(LE(vy, vy_o));
		}
		else
		{
			v_max = ry_max;
			vx = v_max*cos¥è;
			ASSERT(LE(vx, vx_o));
			// vy = vy;
		}
	}
	ASSERT(IsSame(sqrt(vx*vx+vy*vy), v_max));
	return;
}

void getResultants(const MotionDef& X, const MotionDef& Y, double angle_rad, double& j, double& a, double& v)
{
	double cos¥è = Abs(cos(angle_rad));
	if (IsSame(Abs(cos¥è), 1.0))
	{
		j = X.jerk;
		a = X.accl;
		v = X.feed;
	}
	else if (IsZero(cos¥è))
	{
		j = Y.jerk;
		a = Y.accl;
		v = Y.feed;
	}
	else
	{
		double sin¥è = Abs(sin(angle_rad));
		double x(X.feed), y(Y.feed), r;
		v = sqrt(x*x + y*y);
		getElements(v, cos¥è, sin¥è, x, y);
		ASSERT(IsSame(sqrt(x*x+y*y), v));

		// Determine the resultant jerk
		r = min(X.jerk, Y.jerk);
		x = r*cos¥è, y = r*sin¥è;
		j = sqrt(x*x + y*y);

		// Determine the resultant acceleration
		r = min(X.accl, Y.accl);
		x = r*cos¥è, y = r*sin¥è;
		a = sqrt(x*x + y*y);
	}
	return;
}


//////////////////////////////////////////////////////////////////////////

int SLProfile::generateXY(const MotionDef& axisX, const MotionDef& axisY, double angle_rad, double distance)
{
	int nRet = checkMotionDef(axisX);
	if (PROFILE_OK != nRet)
	{
		ASSERT(FALSE);
		return nRet;
	}
	nRet = checkMotionDef(axisY);
	if (PROFILE_OK != nRet)
	{
		ASSERT(FALSE);
		return (nRet+3);
	}

	clear();

	getResultants(axisX, axisY, angle_rad, Jerk, Amax, Fmax);

	return fitToDist(distance);
}

int SLProfile::generateZ(const MotionDef& axis, double distance)
{
	int nRet = checkMotionDef(axis);
	if (PROFILE_OK != nRet)
	{
		ASSERT(FALSE);
		return (nRet+6);
	}
	if (LE(distance, 0))
	{
		ASSERT(FALSE);
		return PROFILE_ERR_INPUT_DISTANCE;
	}

	clear();

	Jerk = axis.jerk;
	Amax = axis.accl;
	Fmax = axis.feed;

	return fitToDist(distance);
}

bool SLProfile::residualForTime(double time, double& distance) const
{
	if (!hasGenerated() || LE(time, 0))
	{
		ASSERT(FALSE);
		return false;
	}

	//
	// Check if unusually long time is provided,
	// where we don't have to calculate the residual for the time
	//
	if (GT(time, timeTotal()))
	{	// if time allowed is longer than the whole profile time,
		// the profile full distance can be accommodated within the travel 'time'.
		distance = distTotal();
		return true;
	}

	//
	// Check the possible distance backward in time from the destination point
	//
	double T_elapsed = 0;
	double S_traveled = 0;

	// First, the final acceleration section within the slowing down interval
	// near the target position
	if (LE(time, Tj))
	{	// if time allowed is shorter than jerk time
		// then the jerk distance in the final acceleration section is the residual
		distance = (1.0/6.0)*Jerk*pow(time, 3.0);
		return true;
	}
	// else accumulate the time and distance traveled
	// for the last acceleration section of the slowing down interval
	T_elapsed += Tj;
	S_traveled += dSj();

	//
	// Check if a constant deceleration section exists in the slowing down interval
	//
	if (0 < Ta)
	{	// if so, check if the time allowed can be accommodated within
		// the constant deceleration section.
		if (LE(time, T_elapsed+Ta))
		{	// then calculate the corresponding distance in two sections
			double t = time - T_elapsed;
			ASSERT(0 < t && LE(t, Ta));
			distance = S_traveled + 0.5*Amax*t*t + dVj()*t;
			return true;
		}
		// else accumulate the time and distance traveled so far
		T_elapsed += Ta;
		S_traveled +=  dSa();
	}
	ASSERT(GT(time, T_elapsed));

	//
	// Check the next backward section,
	// the deceleration section of the beginning of slowing down interval
	//
	if (LE(time, T_elapsed + Tj))
	{
		double t = time - T_elapsed;
		ASSERT(0 < t && LE(t, Tj));
		distance = S_traveled + (Fmax*t-(1.0/6.0)*Jerk*(pow(t, 3.0) + 3*t*Tj*(Tj-t)));
		return true;
	}
	T_elapsed += Tj;
	S_traveled += (Fmax*Tj - dSj());
	ASSERT(IsSame(S_traveled, Fmax*Tj+dSa()));

	ASSERT(GT(time, T_elapsed));

	//
	// Check if the constant feed rate section exists
	//
	if (0 < Tv)
	{	// if so, check if the time allowed can be accommodated within
		// the constant feed rate section.
		if (LE(time, T_elapsed+Tv))
		{
			double t = time - T_elapsed;
			ASSERT(0 < t && LE(t, Tv));
			distance = S_traveled + Fmax*t;
			return true;
		}
		T_elapsed += Tv;
		S_traveled += (Fmax*Tv);
	}

	ASSERT(GT(time, T_elapsed));

	//
	// Check if the last deceleration section of speeding up interval has the target point
	//
	if (LE(time, T_elapsed+Tj))
	{
		double t = time - T_elapsed;
		ASSERT(0 < t && LE(t, Tj));
		distance = S_traveled + Fmax*t - (1.0/6.0)*Jerk*pow(t, 3.0);
		return true;
	}
	T_elapsed += Tj;
	S_traveled += (Fmax*Tj - dSj());

	ASSERT(GT(time, T_elapsed));

	//
	// Check if a constant acceleration section exists in the speeding up interval
	//
	if (0 < Ta)
	{
		if (LE(time, T_elapsed+Ta))
		{	// check if the time allowed can be accommodated within the constant acceleration section.
			double t = time - T_elapsed;
			ASSERT(0 < t && LE(t, Ta));
			distance = S_traveled + (Fmax - dVj())*t - 0.5*Amax*t*t;
			return true;
		}
		T_elapsed += Ta;
		S_traveled +=  dSa();
	}

	ASSERT(GT(time, T_elapsed));

	if (LE(time, T_elapsed + Tj))
	{
		double t = time - T_elapsed;
		ASSERT(0 < t && LE(t, Tj));
		distance = S_traveled + (dSj() - (1.0/6.0)*Jerk*pow(Tj-t, 3.0));
		return true;
	}
	ASSERT(GT(time, timeTotal()));

	distance = distTotal();
	return true;
}

int SLProfile::init()
{
	// The default profile
	//  1) MUST have the jerk-applied acceleration/deceleration segments,
	//  2) CAN have constant acceleration/deceleration segments according to allowed jerk and acceleration,
	//  3) and does NOT have constant feed rate segment YET.

	if (LE(Jerk, 0) || LE(Amax, 0) || LE(Fmax, 0))
	{
		ASSERT(FALSE);
		return PROFILE_ERR_NOT_INITIALIZED;
	}

	double Sum¥ÄVj = Amax*Amax/Jerk;

	if (GT(Fmax, Sum¥ÄVj))	// maximum allowed speed is greater than the sum of
	{						// jerk accelerated speed and jerk decelerated speed,
		// which means a constant acceleration segment must exist
		Tj = Amax/Jerk;
		Ta = (Fmax-Sum¥ÄVj)/Amax;
		ASSERT(GT(Ta, 0.0));
		Tv = 0;
	}
	else					// we need to adjust maximum acceleration such that
	{						// the sum of accelerated/decelerated speeds falls within the speed limit
		if (LT(Fmax, Sum¥ÄVj))
			Amax = sqrt(Jerk*Fmax);
		Tj = Amax/Jerk;
		Ta = Tv = 0;
	}

	return PROFILE_OK;
}

int SLProfile::fitToDist(double distance)
{
	// First, generate the default profile based on machine settings.
	// The default profile
	// i.  MUST have the jerk-applied acceleration/deceleration segments,
	// ii. CAN have constant acceleration/deceleration segments according to allowed jerk and acceleration,
	// iii. and does NOT have constant feed rate segment YET.
	if (PROFILE_OK != init())
		return PROFILE_ERR_NOT_INITIALIZED;
	ASSERT(0 == Tv);

	double Stotal = distTotal();

	//
	// And then complete the profile according to the given travel distance
	//
	if (GT(distance, Stotal))			// constant feed rate segment exists
	{
		double dist_diff = distance - Stotal;
		Tv = dist_diff/Fmax;
	}
	else if (GT(Stotal, distance))
	{	// Without the constant feed rate segment,
		// total distance = acc_distance + constant acceleration distance + dec_distance
		//	= 2*Fmax*Tj + Fmax*Ta

		double ¥ÄVj = dVj();
		double Saccdec = 4*¥ÄVj*Tj;	// distance traveled only by acceleration & deceleration

		if (LT(Saccdec, distance))		// constant acceleration segments exist
		{	// no changes in jerk-applied segments
			// but adjustment on constant acceleration and, as such, on the max feed rate as well
			ASSERT(0 < Ta);
			double b = ¥ÄVj + Amax*Tj;
			Ta = (-b + sqrt(b*b+Amax*(distance-4*¥ÄVj*Tj)))/Amax;
			Fmax = 2*¥ÄVj + Amax*Ta;
			ASSERT(IsSame(Fmax*Tj + 0.5*Fmax*Ta, 0.5*distance));
		}
		else	// only acceleration-deceleration and deceleration-acceleration
		{
			Ta = 0;
			if (LT(distance, Saccdec))
			{	// need to readjust jerk application time, and, in turn, the max acceleration and max feed rate
				double half_dist = 0.5*distance;
				Tj = pow(half_dist/Jerk, 1.0/3.0);
				Amax = Jerk*Tj;
				Fmax = Amax*Tj;	// Fmax = 2¥ÄVj = 2((1/2)Jerk*Tj^2) = Jerk*Tj^2 = Amax*Tj
				ASSERT(IsSame(dVj(), 0.5*Fmax));
			}
			else
			{	// the max feed rate needs to be adjusted such that the profile has no constant acceleration segment
				ASSERT(IsSame(distance, Saccdec));
				Fmax = 2*¥ÄVj;
				ASSERT(IsSame(Amax*Tj, Fmax));	// Amax*Tj = Jerk*Tj*Tj = 2(1/2)Jerk*Tj*Tj = 2¥ÄVj
			}
			ASSERT(IsSame(2*Fmax*Tj, distance));
		}
	}
	// else distance == Stotal
	// => then no change from the profile generated with the machine settings
	//

	ASSERT(IsSame(distTotal(), distance));

	return PROFILE_SUCCESS;
}

int SLProfile::fitToTime(double time)
{
	if (LE(time, 0))
	{
		ASSERT(FALSE);
		return PROFILE_ERR_INPUT_TIME;
	}
	if (!hasGenerated())
	{
		ASSERT(FALSE);
		return PROFILE_ERR_NOT_INITIALIZED;
	}

	if (IsSame(timeTotal(), time))
		return PROFILE_SUCCESS;

	double halfLimit = 0.5*time;

	if (GE(2.0*Tj, halfLimit))
	{
		Tj = time/4.0;
		Ta = Tv = 0;
	}
	else if (GE(timeAccel(), halfLimit))
	{
		double dT = timeAccel() - halfLimit;
		ASSERT(GE(Ta, dT) && IsSame(2.0*Tj+Ta-dT, halfLimit));	// because 2*Tj is already less than halfLimit
		Ta -= dT;
		Tv = 0;
	}
	else if (GE(timeTotal(), time))
	{
		double dT = timeTotal() - time;
		ASSERT(0 < Tv && GE(Tv, dT) && IsSame(2.0*timeAccel()+Tv-dT, time));
		Tv -= dT;
	}
	else
	{
		Tv = time - 2.0*timeAccel();
	}

	ASSERT(IsSame(Tj+Ta+Tj + Tv + Tj+Ta+Tj, time));

	return PROFILE_SUCCESS;
}


//////////////////////////////////////////////////////////////////////////

CLProfile::CLProfile()
{
	memset(this, 0, sizeof(CLProfile));
}

void CLProfile::clear()
{
	memset(this, 0, sizeof(CLProfile));
}

int CLProfile::generate(const CLParam param)
{
	int nRet = checkMotionDef(param.axisX);
	if (PROFILE_OK != nRet)
	{
		ASSERT(FALSE);
		return nRet;
	}
	nRet = checkMotionDef(param.axisY);
	if (PROFILE_OK != nRet)
	{
		ASSERT(FALSE);
		return (nRet+3);
	}
	if (0 > param.initialFeed || 0 > param.finalFeed)
	{
		ASSERT(FALSE);
		return PROFILE_ERR_INPUT_FEEDS;
	}

	clear();

	double j = 0, a = 0, v = 0;
	getResultants(param.axisX, param.axisY, param.xyAngleRad, j, a, v);
	if (LE(j, 0) || LE(a, 0) || LE(v, 0) || GT(param.initialFeed, v) || GT(param.finalFeed, v))
	{
		ASSERT(FALSE);	// never happens
		return PROFILE_INVALID_INPUT;
	}

	Jerk = j;
	Fmax = v;
	Amax = a;

	F_i = param.initialFeed;
	F_f = param.finalFeed;

	return fitToDistance(param.xyDistance);
}

void determineScurve(const double Sum¥ÄVj, const double jerk, const double a_max, const double ¥Äv_total, double& tj, double& a, double& ta)
{
	if (GT(¥Äv_total, Sum¥ÄVj))// maximum allowed speed is greater than the sum of
	{						// jerk accelerated speed and jerk decelerated speed,
		tj = a_max/jerk;	// which means a constant acceleration segment must exist
		ta = (¥Äv_total-Sum¥ÄVj)/a_max;
		ASSERT(GT(ta, 0.0));
		a = a_max;
	}
	else					// we need to adjust maximum acceleration such that
	{	 					// the sum of accelerated/decelerated speeds falls within the speed limit
		if (LT(¥Äv_total, Sum¥ÄVj))
		{	// then need to adjust maximum acceleration
			// such that sum of ¥ÄVj's be equals to ¥Äv_total, that is, ¥Äv_total -> Sum¥ÄVj
			a = sqrt(jerk*¥Äv_total);	// a = j*t, dv = 2*dvj = 2*((1/2)j*t^2) = j*t^2 = (j*t)^2/j => (j*t)^2 = a^2 = j*dv
		}
		else
		{
			ASSERT(IsSame(¥Äv_total, Sum¥ÄVj));
			a = a_max;
		}
		tj = a/jerk;
		ta = 0;
	}
	return;
}

bool CLProfile::init()
{
	// The default profile
	//  1) MUST have the jerk-applied acceleration/deceleration segments,
	//  2) CAN have constant acceleration/deceleration segments according to allowed jerk and acceleration,
	//  3) and does NOT have constant feed rate segment YET.

	if (LE(Jerk, 0) || LE(Fmax, 0) || LE(Amax, 0) || GT(F_i, Fmax) || GT(F_f, Fmax))
	{
		ASSERT(FALSE);
		return false;
	}

	double Sum¥ÄVj = Amax*Amax/Jerk;

	//
	// First, the former speeding up S-curve segment
	//
	if (LT(F_i, Fmax))
		determineScurve(Sum¥ÄVj, Jerk, Amax, Fmax-F_i, Tj1, Amax1, Ta1);
	else
		Tj1 = Amax1 = Ta1 = 0;

	//
	// And then, the latter speeding down S-curve segment
	//
	if (LT(F_f, Fmax))
		determineScurve(Sum¥ÄVj, Jerk, Amax, Fmax-F_f, Tj2, Amax2, Ta2);
	else
		Tj2 = Amax2 = Ta2 = 0;

	//
	// Finally, let the constant feed rate segment not exist
	// and the highest possible feed late be the same as the maximum given feed rate
	//
	Tv = 0;
	V_max = Fmax;

	return GT(Tj1 + Tj2, 0);
}

int CLProfile::fitToDistance(double distance)
{
	// First, generate the default profile based on machine settings.
	// The default profile
	// i.  MUST have the jerk-applied acceleration/deceleration segments,
	// ii. CAN have constant acceleration/deceleration segments according to allowed jerk and acceleration,
	// iii. and does NOT have constant feed rate segment YET.
	if (!init())
		return PROFILE_ERR_NOT_INITIALIZED;
	ASSERT(0 == Tv);

	const double Stotal = distTotal();

	//
	// And then complete the profile according to the given travel distance
	//
	if (GT(distance, Stotal))	// no changes in the default motion parameters of the motor axes
	{							// and the constant feed rate segment exists
		ASSERT(IsSame(V_max, F_i+2*dVj1()+dVa1())
			   && IsSame(V_max, 2*dVj2()+dVa2()+F_f));
		Tv = (distance - Stotal)/V_max;
	}
	else if (GT(Stotal, distance))
	{	// Without the constant feed rate segment,
		// total distance = acc_distance + constant acceleration distance + dec_distance
		//	= 2*Fmax*Tj + Fmax*Ta
		adjustMaxFeed(distance);
	}
	// else distance == Stotal
	// => then no change from the profile generated with the machine settings
	//

	ASSERT(IsSame(distTotal(), distance));

	return PROFILE_SUCCESS;
}

double CLProfile::getDistanceFor(double maxFeed, double iniFeed, double& Tj_, double& Accel_, double& Ta_)
{
	ASSERT(LE(maxFeed, Fmax) && LE(iniFeed, maxFeed));

	double dvMax = maxFeed - iniFeed;

	Tj_ = sqrt(dvMax/Jerk);	// first, get the max jerk time with the relation of
							// 2*dVj == max feed rate (with zero initial feed) <=> 2(1/2)Jerk*Tj_^2 = dvMax

	if (GT(Jerk*Tj_, Amax))	// then the assumed jerk time exceeds the allowed limit,
	{						// which means the constant acceleration segment must exist
		Tj_ = Amax/Jerk;
		Accel_ = Amax;

		double dvJ = 0.5*Jerk*pow(Tj_, 2.0);
		double dva = dvMax - (dvJ + dvJ);
		ASSERT(GT(dva, 0));
		Ta_ = dva/Accel_;
	}
	else
	{
		Accel_ = Jerk*Tj_;
		Ta_ = 0;
	}

	return (iniFeed + maxFeed)*(Tj_+0.5*Ta_);
}

void CLProfile::adjustMaxFeed(const double distance)
{
	ASSERT(hasGenerated() && GT(distTotal(), distance) && 0 == Tv);

	const double c_distance_epsilon = 0.0001;	// 0.1um
	double v_lo = maxof(F_i, F_f);
	double v_hi = Fmax, S = 0;
	ASSERT(0 <= v_lo);

	V_max = (v_lo + v_hi)/2;
	S = getDistanceFor(V_max, F_i, Tj1, Amax1, Ta1)
			+ getDistanceFor(V_max, F_f, Tj2, Amax2, Ta2);
	ASSERT(IsSame(distTotal(), S));

	while (GT(diff(distance, S), c_distance_epsilon))
	{
		if (LT(S, distance))
			v_lo = V_max;
		else
			v_hi = V_max;
		V_max = (v_lo + v_hi)/2;

		S = getDistanceFor(V_max, F_i, Tj1, Amax1, Ta1)
			+ getDistanceFor(V_max, F_f, Tj2, Amax2, Ta2);
		ASSERT(IsSame(distTotal(), S));
	}

	return;
}



//////////////////////////////////////////////////////////////////////////

FlyingProfile::FlyingProfile() : heightToAscend(0), distanceToDescend(0), residualForStartDescending(0), upDownSyncWaitTime(0)
{
	resultantJumpType = nJUMPTYPE_NONE;
}

FlyingProfile::~FlyingProfile()
{
}

int FlyingProfile::jumpInfo(double& ascendingDist, double& residualForDescending, double& descendingDist) const
{
	ascendingDist = heightToAscend;
	descendingDist = distanceToDescend;
	residualForDescending = residualForStartDescending;
	return resultantJumpType;
}


int FlyingProfile::generate(const FlyingParam& param)
{
	clear();

	//
	// First, get time to move the X-Y axes to the target position
	//
	int nRet = xyProfile.generateXY(param.motorX, param.motorY, param.jump.xyAngleRad, param.jump.xyDistance);
	if (PROFILE_SUCCESS != nRet)
		return nRet;

	//
	// Secondly, check if we need to generate profiles to get flying mode jump parameters
	//
	flyingCond cond = checkFlyingCondition(param);
	switch (cond)
	{
	case invalidInput:			return PROFILE_INVALID_INPUT;		break;
	case notFlyingMode:			return PROFILE_ERR_NOT_FLYINGMODE;	break;
	case flyingNotApplicable:	return PROFILE_ERR_NOT_FLYINGMODE;	break;
	default: ASSERT(conditionOk == cond);	break;
	}
	ASSERT(GE(heightToAscend, 0) && GE(distanceToDescend, 0));

	//
	// Determine the desired time for a jump
	//
	double timeTotal = xyProfile.timeTotal();
	// Check if it's needed to start to jump earlier than calculated theoretical time.
	// so as to make the jump-end position coincide with x-y target position
	upDownSyncWaitTime = (0 < param.jumpSyncDelay)? (0.001*param.jumpSyncDelay): 0.0;
	if (GT(upDownSyncWaitTime, 0))
	{	// need to start descending motion upDownSyncWaitTime earlier than the theoretically exact descending-beginning time
		if (GT(timeTotal, upDownSyncWaitTime))
		{
			timeTotal -= upDownSyncWaitTime;	// jump time decreases by the synchronization time
		}
		else
		{	// no time to jump, just move to the destination point
			heightToAscend = maxof(0.0, zJumpEnd-zJumpStart);
			distanceToDescend = 0;
		}
	}

	//
	// 1. the x-y speed profile has been generated
	// 2. jump type has been determined and
	// 3. jump time is just calculated,
	// so we are ready to generate the motion planning/profiles for a flying mode jump
	//
	if (IsZero(heightToAscend) || IsZero(distanceToDescend))
		return residualOnHalfOrNoJump(param.motorZ, timeTotal);

	ASSERT(!zUpProfile.hasGenerated() && !zDownProfile.hasGenerated());
	//
	// Get z-axis up and down profiles
	//
	VERIFY(PROFILE_SUCCESS == zUpProfile.generateZ(param.motorZ, heightToAscend));
	VERIFY(PROFILE_SUCCESS == zDownProfile.generateZ(param.motorZ, distanceToDescend));
	double t_sum = zUpProfile.timeTotal() + zDownProfile.timeTotal();

	if (LE(t_sum, timeTotal))
	{	// time to travel is long enough to accommodate both of jump up and down
		// return the distance during the down profile time
		VERIFY(xyProfile.residualForTime(zDownProfile.timeTotal(), residualForStartDescending));
		resultantJumpType = nJUMPTYPE_FLYING;
		return PROFILE_SUCCESS;
	}
	else
	{	// time to travel is shorter than the sum of jump up and down
		// need to adjust jump height according to the time allowed for travel
		return adjustJumpUpDownProfile(param.motorZ, timeTotal, t_sum);
	}
}


int FlyingProfile::adjustJumpUpDownProfile(const MotionDef& axisZ, const double timeTotal, double t_sum)
{
	const double c_time_epsilon = 0.00001;
	double dzUp = 0, dzDown = 0, tDown = 0;

	double z_max = zJumpStart + heightToAscend, z_min = zJumpEnd;
	ASSERT(IsSame(z_max, zJumpEnd + distanceToDescend));
	double z_height = z_max;

	do 
	{
		if (t_sum > timeTotal)
			z_max = z_height;
		else
			z_min = z_height;

		z_height = (z_max + z_min)/2;

		dzUp = z_height - zJumpStart;
		dzDown = z_height - zJumpEnd;
		if (LE(dzUp, 0) || LE(dzDown, 0))
			return finishJumpHeightBiSect(axisZ, timeTotal, z_height);

		VERIFY(PROFILE_SUCCESS == zUpProfile.generateZ(axisZ, dzUp));
		VERIFY(PROFILE_SUCCESS == zDownProfile.generateZ(axisZ, dzDown));

		tDown = zDownProfile.timeTotal();
		t_sum = zUpProfile.timeTotal() + tDown;
	}
	while (GT(Abs(t_sum-timeTotal), c_time_epsilon) || GT(t_sum, timeTotal));

	heightToAscend = dzUp;
	distanceToDescend = dzDown;

	VERIFY(xyProfile.residualForTime(tDown+upDownSyncWaitTime, residualForStartDescending));

	resultantJumpType = nJUMPTYPE_FLYING;
	return PROFILE_SUCCESS;
}


FlyingProfile::flyingCond FlyingProfile::checkFlyingCondition(const FlyingParam& param)
{
	zJumpStart = param.zJumpStart;
	zJumpEnd = param.zJumpTarget;
	heightToAscend = ZeroLimit(param.zJumpHeight - zJumpStart);
	distanceToDescend = ZeroLimit(param.zJumpHeight - zJumpEnd);

	// Check integrity of the jump profile
	if (0 > distanceToDescend)	// jump height is never lower than the destination height
	{
		ASSERT(FALSE);
		return invalidInput;
	}
	// meanwhile, the jump height can be lower than the cutting end position
	// only if it is higher than or equal to the destination height
	if (IsZero(heightToAscend) || IsZero(distanceToDescend))
	{
		residualForStartDescending = 0;
		resultantJumpType = nJUMPTYPE_HALFFLYING;
		return flyingNotApplicable;
	}

	// Check jump options that have effect on the jump profile
	if (LE(param.jump.xyDistance, param.minJumpRadius))
	{	// distance to the next spot is less than the minimum distance for jumping
		if (GE(zJumpEnd, zJumpStart))
		{
			heightToAscend = zJumpEnd - zJumpStart;
			distanceToDescend = 0;
			residualForStartDescending = 0;

			zUpProfile.generateZ(param.motorZ, heightToAscend);
			zDownProfile.clear();
		}
		else
		{
			heightToAscend = 0;
			distanceToDescend = zJumpStart - zJumpEnd;
			residualForStartDescending = param.jump.xyDistance;

			zUpProfile.clear();
			zDownProfile.generateZ(param.motorZ, distanceToDescend);
		}
		resultantJumpType = nJUMPTYPE_HALFFLYING;
		return flyingNotApplicable;
	}
	if (GT(param.maxJumpRadius, 0) && GE(param.jump.xyDistance, param.maxJumpRadius))
	{	// distance to the next spot is too long to ensure a safe flying landing. Run this in half-flying mode
		zUpProfile.generateZ(param.motorZ, heightToAscend);
		zDownProfile.clear();
		residualForStartDescending = 0;
		resultantJumpType = nJUMPTYPE_HALFFLYING;
		return flyingNotApplicable;	// half-flying
	}

	resultantJumpType = nJUMPTYPE_FLYING;
	return conditionOk;
}


int FlyingProfile::residualOnHalfOrNoJump(const MotionDef& Z, const double timeTotal)
{
	ASSERT(xyProfile.hasGenerated() && xyProfile.timeTotal() == timeTotal+upDownSyncWaitTime);

	if (! (IsZero(distanceToDescend) || IsZero(heightToAscend)))
	{
		ASSERT(FALSE);
		resultantJumpType = nJUMPTYPE_NONE;
		return PROFILE_INVALID_INPUT;
	}

	if (IsZero(distanceToDescend))
	{	// XY distance is too short to make a flying jump
		zDownProfile.clear();
		residualForStartDescending = 0;

		resultantJumpType = nJUMPTYPE_HALFFLYING;

		if (IsZero(heightToAscend))
		{
			zUpProfile.clear();
			return PROFILE_SUCCESS;
		}
		else
		{
			return zUpProfile.generateZ(Z, Abs(heightToAscend));
		}
	}

	ASSERT(true == IsZero(heightToAscend) && false == IsZero(distanceToDescend));
	zUpProfile.clear();
	int nRet = zDownProfile.generateZ(Z, Abs(distanceToDescend));
	if (PROFILE_SUCCESS != nRet)
	{
		resultantJumpType = nJUMPTYPE_NONE;
		return nRet;
	}

	double t = zDownProfile.timeTotal();
	ASSERT(0 < t);
	if (GE(timeTotal, t))
	{
		VERIFY(xyProfile.residualForTime(t+upDownSyncWaitTime, residualForStartDescending));
	}
	else
	{
		residualForStartDescending = xyProfile.distTotal();
	}

	resultantJumpType = nJUMPTYPE_FLYING;
	return PROFILE_SUCCESS;
}


int FlyingProfile::finishJumpHeightBiSect(const MotionDef& Z, const double timeTotal, double curr_zHeight)
{
	if (curr_zHeight < zJumpStart)
	{
		heightToAscend = ZeroLimit(zJumpEnd - zJumpStart);
		distanceToDescend = ZeroLimit(heightToAscend + zJumpStart - zJumpEnd);
		if (IsZero(heightToAscend) || IsZero(distanceToDescend))
			return residualOnHalfOrNoJump(Z, timeTotal);

		ASSERT(IsSame(heightToAscend + zJumpStart, distanceToDescend+zJumpEnd));

		VERIFY(PROFILE_SUCCESS == zUpProfile.generateZ(Z, Abs(heightToAscend)));
		VERIFY(PROFILE_SUCCESS == zDownProfile.generateZ(Z, Abs(distanceToDescend)));
		double t1 = zUpProfile.timeTotal(), t2 = zDownProfile.timeTotal();
		if (LE(t1+t2, timeTotal))
			VERIFY(xyProfile.residualForTime(t2+upDownSyncWaitTime, residualForStartDescending));
		else if (LT(t1, timeTotal))
			VERIFY(xyProfile.residualForTime(timeTotal-t1+upDownSyncWaitTime, residualForStartDescending));
		else
			residualForStartDescending = 0;
		return PROFILE_SUCCESS;
	}

	ASSERT(LE(curr_zHeight, zJumpEnd));
	distanceToDescend = 0;
	heightToAscend = ZeroLimit(zJumpEnd - zJumpStart);
	return residualOnHalfOrNoJump(Z, timeTotal);
}

inline void movebuf(Trace*& pFrom, Trace*& pTo)
{
	pTo = pFrom;
	pFrom = NULL;
}

bool FlyingProfile::trace(FlyingTrace& buf) const
{
	if (!canTrace())
	{
		ASSERT(FALSE);
		return false;
	}

	ProfileData profile;
	TraceBuf data;

	if (!profile.trace(xyProfile, buf.ms_sampling, data))
		return false;
	ASSERT(0 < data.trace_count);

	buf.trace_count = data.trace_count;
	movebuf(data.p, buf.pXY);
	data.trace_count = 0;

	if (!profile.trace(*this, buf.ms_sampling, data))
		return false;

	ASSERT(data.trace_count == buf.trace_count);
	movebuf(data.p, buf.pZ);
	data.trace_count = 0;

	return true;
}


//////////////////////////////////////////////////////////////////////////

TraceBuf::TraceBuf()
{
	memset(this, 0, sizeof(TraceBuf));
}

TraceBuf::~TraceBuf()
{
	if (p)
	{
		delete[] p;
	}
}

void TraceBuf::clear()
{
	if (p)
		delete[] p;
	memset(this, 0, sizeof(TraceBuf));
}

bool TraceBuf::alloc(double period, UINT sampling_interval_ms)
{
	if (0 == sampling_interval_ms || LE(period, 0))
	{
		ASSERT(FALSE);
		return false;
	}

	if (p)
		clear();
	trace_count = UINT(1000.0*period/sampling_interval_ms+0.5) + 1;
	p = new Trace[trace_count];
	if (!p)
		return false;
	memset(p, 0, sizeof(Trace)*trace_count);

	return true;
}

void FlyingTrace::clear()
{
	if (pXY || pZ)
	{
		delete[] pXY;
		delete[] pZ;
	}
	memset(this, 0, sizeof(TraceBuf));
}


//////////////////////////////////////////////////////////////////////////

bool ProfileData::init(const SLProfile& profile)
{
	if (!profile.hasGenerated())
	{
		ASSERT(FALSE);
		return false;
	}

	jerk = profile.jerk();
	a_max = profile.accelMax();
	v_max = profile.feedMax();

	double Tj = profile.periodJerkApply();
	double Ta = profile.periodConstAccl();
	double Tv = profile.periodConstFeed();

	_dVj = profile.dVj();

	_Time[PHASE_JERK1] = Tj;
	_Time[PHASE_ACCEL] = _Time[PHASE_JERK1] + Ta;
	_Time[PHASE_JERK2] = _Time[PHASE_ACCEL] + Tj;
	_Time[PHASE_CFEED] = _Time[PHASE_JERK2] + Tv;
	_Time[PHASE_JERK3] = _Time[PHASE_CFEED] + Tj;
	_Time[PHASE_DECEL] = _Time[PHASE_JERK3] + Ta;
	_Time[PHASE_JERK4] = _Time[PHASE_DECEL] + Tj;

	_Dist[PHASE_JERK1] = profile.dSj();
	_Dist[PHASE_ACCEL] = _Dist[PHASE_JERK1] + profile.dSa();
	_Dist[PHASE_JERK2] = profile.dSa() + v_max*Tj;
	_Dist[PHASE_CFEED] = _Dist[PHASE_JERK2] + v_max*Tv;
	_Dist[PHASE_JERK3] = _Dist[PHASE_CFEED] + (v_max*Tj-profile.dSj());
	_Dist[PHASE_DECEL] = _Dist[PHASE_JERK3] + profile.dSa();
	_Dist[PHASE_JERK4] = _Dist[PHASE_DECEL] + profile.dSj();
	ASSERT(IsSame(profile.distTotal(), _Dist[PHASE_JERK4]));

	return true;
}

inline double milliSecondToSecond(UINT ms) { return 0.001*ms; }

bool ProfileData::trace(const SLProfile& profile, UINT sampling_ms, TraceBuf& data)
{
	if (!profile.hasGenerated() || 0 == sampling_ms || !init(profile))
	{
		ASSERT(FALSE);
		return false;
	}

	double timeTotal = profile.timeTotal();
	if (!data.alloc(timeTotal, sampling_ms))
		return false;

	traceStat st = stat_Ready;

	double t = 0;
	UINT timer = 0;

	for (UINT i = 0; i < data.trace_count; ++i)
	{
		t = milliSecondToSecond(timer);
		Trace& tr = data.p[i];

		tr.time_ms = timer;

		if (!trace(st, t, tr))
		{
			ASSERT(stat_Finished == st);
			tr.jerk = 0, tr.accel = 0, tr.feed - 0, tr.distance = 0;
			data.trace_count = i+1;
			break;
		}

		timer += sampling_ms;
	}

	return true;
}

bool ProfileData::trace(const FlyingProfile& profile, UINT sampling_ms, TraceBuf& data)
{
	const SLProfile& up = profile.jumpUpProfile();
	const SLProfile& down = profile.landingProfile();
	double timeTotal = profile.traversingProfile().timeTotal();

	if (!(up.hasGenerated() || down.hasGenerated()) || LE(timeTotal, 0) || 0 == sampling_ms)
	{
		ASSERT(FALSE);
		return false;
	}

	double timeUp = up.timeTotal(), timeDown = down.timeTotal();
	if (GT(timeUp + timeDown, timeTotal))
	{
		ASSERT(FALSE);
		return false;
	}

	if (!data.alloc(timeTotal, sampling_ms))
		return false;

	traceStat st = stat_Ready;

	double t = 0, t_down = 0;
	UINT timer = 0, timer2 = 0, i = 0;

	// Jump upward first
	double z_base = profile.jumpStartHeight();
	if (up.hasGenerated())
	{
		if (!init(up))
		{
			ASSERT(FALSE);
			return false;
		}

		int direction = profile.jumpUpDirection();
		for (; i < data.trace_count; ++i)
		{
			t = milliSecondToSecond(timer);
			if (GT(t, timeUp))
				break;

			Trace& tr = data.p[i];
			tr.time_ms = timer;

			if (!trace(st, t, tr))
			{
				ASSERT(stat_Finished == st);
				break;
			}
			if (0 < direction)
			{
				tr.distance = z_base + tr.distance;
			}
			else
			{
				tr.distance = z_base - tr.distance;
			}

			timer += sampling_ms;
		}
	}

	const double z_height = z_base + profile.distToAscend();
	const double time_start_landing = timeTotal - timeDown - profile.jumpSyncWaitTime();

	// Stay at the peak if total travel time is longer than sum of jump up and down times
	for (; i < data.trace_count; ++i)
	{
		if (GT(t, time_start_landing))
			break;

		Trace& tr = data.p[i];
		tr.time_ms = timer;
		tr.jerk = 0, tr.accel = 0;
		tr.feed = 0;
		tr.distance = z_height;

		timer += sampling_ms;
		t = milliSecondToSecond(timer);
	}

	// Land down to the target spot
	int direction = profile.landingDirection();
	const UINT timeAtDown = timer;

	if (down.hasGenerated())
	{
		if (!init(down))
		{
			ASSERT(FALSE);
			return false;
		}

		st = stat_Ready;

		for (; i < data.trace_count; ++i)
		{
			if (GT(t, timeTotal))
				break;

			Trace& tr = data.p[i];
			tr.time_ms = timer;

			if (!trace(st, t_down, tr))
			{
				ASSERT(stat_Finished == st);
				break;
			}
			if (0 < direction)
			{
				tr.distance = z_height - tr.distance;
			}
			else
			{
				tr.distance = z_height + tr.distance;
			}

			timer += sampling_ms;
			t = milliSecondToSecond(timer);
			t_down = milliSecondToSecond(timer - timeAtDown);
		}
	}
	ASSERT(0 < i && i <= data.trace_count);

	if (i < data.trace_count)
	{
		if (stat_Finished != st && down.hasGenerated() && 0 < t_down)
		{
			for (; i < data.trace_count; ++i)
			{
				if (!trace(st, t_down, data.p[i]))
				{
					ASSERT(stat_Finished == st);
					break;
				}
				data.p[i].time_ms = timer;
				timer += sampling_ms;
				t = milliSecondToSecond(timer);
				t_down = milliSecondToSecond(timer - timeAtDown);
			}
		}
		if (i < data.trace_count)
		{
			double zDestination = profile.landingHeight();
			for (; i < data.trace_count; ++i)
			{
				Trace& tr = data.p[i];
				tr.jerk = 0, tr.accel = 0, tr.feed = 0;
				tr.distance = zDestination;
				tr.time_ms = timer;
				timer += sampling_ms;
			}
		}
	}

	return true;
}

bool ProfileData::trace(traceStat& st, double t, Trace& result)
{
StartSpot:
	switch (st)
	{
	case stat_Ready:
		if (0 < t) { st = stat_jerkAccelUp; goto StartSpot; }
		ASSERT(IsZero(t));
		result.jerk = 0;
		result.accel = 0;
		result.feed = 0;
		result.distance = 0;
		break;
	case stat_jerkAccelUp:
		if (GT(t, _Time[PHASE_JERK1])) { st = stat_constAccelUp; goto StartSpot; }
		ASSERT(0 < t);
		result.jerk = jerk;
		result.accel = jerk*t;
		result.feed = 0.5*jerk*t*t;
		result.distance = jerk*pow(t, 3.0)/6.0;
		break;
	case stat_constAccelUp:
		if (GT(t, _Time[PHASE_ACCEL])) { st = stat_jerkDecelUp; goto StartSpot; }
		ASSERT(GE(t, _Time[PHASE_JERK1]));
		t -= _Time[PHASE_JERK1];
		result.jerk = 0;
		result.accel = a_max;
		result.feed = _dVj + a_max*t;
		result.distance = _Dist[PHASE_JERK1] + (_dVj + 0.5*a_max*t)*t;
		break;
	case stat_jerkDecelUp:
		if (GT(t, _Time[PHASE_JERK2])) { st = stat_constFeedrate; goto StartSpot; }
		ASSERT(GE(t, _Time[PHASE_ACCEL]));
		{
			double dTj = _Time[PHASE_JERK1];
			t -= _Time[PHASE_ACCEL];
			ASSERT(LE(t, dTj));
			result.jerk = -jerk;
			result.accel = a_max - jerk*t;
			result.feed = v_max - 0.5*jerk*pow(t-dTj, 2);
			result.distance =_Dist[PHASE_ACCEL] + v_max*t - (1.0/6.0)*jerk*(pow(t, 3.0)+3*t*dTj*(dTj - t));
		}
		break;
	case stat_constFeedrate:
		if (GT(t, _Time[PHASE_CFEED])) { st = stat_jerkDecelDown; goto StartSpot; }
		ASSERT(GE(t, _Time[PHASE_JERK2]));
		t = ZeroLimit(t - _Time[PHASE_JERK2]);
		result.jerk = 0;
		result.accel = 0;
		result.feed = v_max;
		result.distance = _Dist[PHASE_JERK2] + v_max*t;
		break;
	case stat_jerkDecelDown:
		if (GT(t, _Time[PHASE_JERK3])) { st = stat_constAccelDown; goto StartSpot; }
		ASSERT(GE(t, _Time[PHASE_CFEED]));
		t = ZeroLimit(t - _Time[PHASE_CFEED]);
		ASSERT(LE(t, _Time[PHASE_JERK1]));
		result.jerk = -jerk;
		result.accel = result.jerk*t;
		result.feed = v_max + 0.5*result.jerk*t*t;
		result.distance = _Dist[PHASE_CFEED] + v_max*t + (1.0/6.0)*result.jerk*pow(t, 3.0);
		break;
	case stat_constAccelDown:
		if (GT(t, _Time[PHASE_DECEL])) { st = stat_jerkAccelDown; goto StartSpot; }
		ASSERT(GE(t, _Time[PHASE_JERK3]));
		t = ZeroLimit(t - _Time[PHASE_JERK3]);
		{
			double v = v_max - _dVj;
			result.jerk = 0;
			result.accel = -a_max;
			result.feed = v + result.accel*t;
			result.distance = _Dist[PHASE_JERK3] + 0.5*(v + result.feed)*t;
		}
		break;
	case stat_jerkAccelDown:
		if (GT(t, _Time[PHASE_JERK4]))
		{
			st = stat_Finished;
			return false;
		}
		ASSERT(GE(t, _Time[PHASE_DECEL]));
		{
			t = ZeroLimit(t - _Time[PHASE_DECEL]);
			double dt = _Time[PHASE_JERK1] - t;
			result.jerk = jerk;
			result.accel = result.jerk*t - a_max;
			result.feed = 0.5*result.jerk*dt*dt;
			result.distance = _Dist[PHASE_DECEL] + (_Dist[PHASE_JERK1] - (1.0/6.0)*jerk*pow(dt, 3.0));
		}
		break;
	case stat_Finished:
		return false;
		break;
	default:
		ASSERT(FALSE);
		return false;
		break;
	}

	return true;
}
