#pragma once

//////////////////////////////////////////////////////////////////////////

const double ех = 1.0e-010;
const double еЁ = 3.1415926535897932384626433832795;
const double еЁ2 = 2*еЁ;
const double unit_rad = еЁ/120.0;
const double graph_margin_factor = 0.025;

inline double	Abs(double r) { return (0 > r)? -r: r; }
inline double	diff(double a, double b) { return (a < b)? b-a: a-b; }
inline double	Deg2Rad(double deg) { return еЁ*deg/180.0; }
inline double	Rad2Deg(double rad) { return 180.0*rad/еЁ; }
inline bool		IsZero(double tiny) { return (0 < tiny)? tiny <= ех: -tiny <= ех; }
inline bool		IsNegative(double a) { return (a < -ех); }
inline bool		IsPositive(double a) { return (a > ех); }
inline bool		NonNegative(double a) { return (IsPositive(a) || IsZero(a)); }
inline bool		NonPositive(double a) { return (IsNegative(a) || IsZero(a)); }
inline bool		IsSame(double a, double b) { return (a > b)? (a-b <= ех): (b-a <= ех); }
inline double	ZeroLimit(double v) { return IsZero(v)? 0: v; }
inline void		LimitToZero(double& v) { v = (ех>=Abs(v))? 0: v; }
inline bool		GT(double a, double b) { return (a > b && !IsSame(a, b)); }
inline bool		GE(double a, double b) { return (a > b || IsSame(a, b)); }
inline bool		LT(double a, double b) { return (a < b && !IsSame(a, b)); }
inline bool		LE(double a, double b) { return (a < b || IsSame(a, b)); }
inline int		ToInt(double v) { return (0 > v)? int(v-0.5): int(v+0.5); }

inline double Mag(double value) { return (IsZero(value)? 0: (0 > value)? -value: value); }

inline double maxof(double a, double b)
{
	return (a < b)? b: a;
}

inline double minof(double a, double b)
{
	return (a < b)? a: b;
}

inline int maxof(int a, int b)
{
	return (a < b)? b: a;
}

inline int minof(int a, int b)
{
	return (a < b)? a: b;
}

inline void Swap(double a, double b)
{
	double t = a;
	a = b;
	b = t;
}

inline double NormalizeRad(double radian)
{
	while (0 > radian)
		radian += еЁ2;
	LimitToZero(radian);
	return (еЁ2 < radian)? fmod(radian, еЁ2): radian;
}


//////////////////////////////////////////////////////////////////////////

struct Point2d
{
	double x, y;

	Point2d();
	Point2d(double x, double y);
	Point2d(const Point2d& other);

	bool isNull() const { return (IsSame(x, 0) && IsSame(y, 0)); }

	// Vector algebra
	double	Dot(const Point2d& t) const { return (x*t.x + y*t.y); }
	double	Cross(const Point2d& t) const { return (x*t.y - y*t.x); }
	double	Distance(const Point2d& t) const { return sqrt((x-t.x)*(x-t.x) + (y-t.y)*(y-t.y)); }
	double	Distance(double X, double Y) const { return sqrt((x-X)*(x-X) + (y-Y)*(y-Y)); }
	double	Magnitude() const { return sqrt(x*x + y*y); }
	double  AngleBetween(const Point2d& t) const;

	// Geometrical algebra
	const Point2d&	operator=(const Point2d& other);
	Point2d			operator+(const Point2d& op) const;
	Point2d			operator-(const Point2d& op) const;
	const Point2d&	operator+=(const Point2d& op);
	const Point2d&	operator-=(const Point2d& op);
	bool			operator==(const Point2d& other) const;
	bool			operator!=(const Point2d& other) const;
};

struct Rect2d
{
	double x1, y1, x2, y2;

	Rect2d() : x1(0), y1(0), x2(0), y2(0) {}
	Rect2d(double X1, double Y1, double X2, double Y2) : x1(X1), y1(Y1), x2(X2), y2(Y2) {}
	Rect2d(const Rect2d& t) { if (this != &t) memcpy(this, &t, sizeof(Rect2d)); }

	void Empty() { x1 = x2 = y2 = y1 = 0; }
	bool isEmpty() const { return (GE(x1, x2) || GE(y1, y2)); }

	double	width() const { return (x2-x1); }
	double	height() const { return (y2-y1); }
	int		Width(double scale) const { return int(scale*width()+0.5); }
	int		Height(double scale) const { return int(scale*height()+0.5); }
	void	Normalize();
	void	Union(double x, double y);
	void	Union(const Rect2d& rect);
	bool	isIntersect(const Rect2d& rect) const;
	bool	Intersect(const Rect2d& rect);
	bool	isInside(const Rect2d& outter) const;
	bool	hasPoint(const Point2d& pt) const;
	void	Translate(double dx, double dy);
	Point2d Center() const { return Point2d(0.5*(x1+x2), 0.5*(y1+y2)); }

	bool operator==(const Rect2d& t) const { return (IsSame(x1, t.x1) && IsSame(x2, t.x2) && IsSame(y1, t.y1) && IsSame(y2, t.y2)); }
	bool operator!=(const Rect2d& t) const { return (!IsSame(x1, t.x1) || !IsSame(x2, t.x2) || !IsSame(y1, t.y1) || !IsSame(y2, t.y2)); }
};

//////////////////////////////////////////////////////////////////////////

inline						Point2d::Point2d() {}
inline						Point2d::Point2d(double x1, double y1) : x(x1), y(y1) {}
inline						Point2d::Point2d(const Point2d& other) : x(other.x), y(other.y) {}
inline const	Point2d&	Point2d::operator=(const Point2d& other) { if (&other != this) { x = other.x, y = other.y; }	return *this; }
inline			Point2d		Point2d::operator+(const Point2d& op) const { return Point2d(x+op.x, y+op.y); }
inline			Point2d		Point2d::operator-(const Point2d& op) const { return Point2d(x-op.x, y-op.y); }
inline const	Point2d&	Point2d::operator+=(const Point2d& op) { x += op.x, y += op.y; return *this; }
inline const	Point2d&	Point2d::operator-=(const Point2d& op) { x -= op.x, y -= op.y; return *this; }
inline			bool		Point2d::operator==(const Point2d& other) const { return (IsSame(x, other.x) && IsSame(y, other.y)); }
inline			bool		Point2d::operator!=(const Point2d& other) const { return (!IsSame(x, other.x) || !IsSame(y, other.y)); }
inline          double      Point2d::AngleBetween(const Point2d& t) const
{
	double AB = this->Magnitude()*t.Magnitude();
	if (IsZero(AB))
	{
		ASSERT(FALSE);
		return 0;
	}
	return NormalizeRad(acos(Dot(t)/AB));
}
inline			void		Rect2d::Union(double x, double y) { x1 = minof(x1, x), x2 = maxof(x2, x); y1 = minof(y1, y), y2 = maxof(y2, y); }
inline			void		Rect2d::Union(const Rect2d& rect) { x1 = minof(x1, rect.x1), x2 = maxof(x2, rect.x2); y1 = minof(y1, rect.y1), y2 = maxof(y2, rect.y2); }
inline void Rect2d::Normalize()
{
	if (x1 > x2)
		Swap(x1, x2);
	if (y1 > y2)
		Swap(y1, y2);
}
inline bool Rect2d::isIntersect(const Rect2d& rect) const
{
	return (maxof(x1, rect.x1) < minof(x2, rect.x2) && maxof(y1, rect.y1) < minof(y2, rect.y2));
}
inline bool Rect2d::Intersect(const Rect2d& rect)
{
	x1 = maxof(x1, rect.x1), x2 = minof(x2, rect.x2);
	y1 = maxof(y1, rect.y1), y2 = minof(y2, rect.y2);
	return (x1 < x2 && y1 < y2);
}
inline bool Rect2d::isInside(const Rect2d& outter) const
{
	return (!isEmpty() && outter.x1 <= x1 && x2 <= outter.x2 && outter.y1 <= y1 && y2 <= outter.y2);
}
inline bool Rect2d::hasPoint(const Point2d& pt) const
{
	return (x1 <= pt.x && pt.x <= x2 && y1 <= pt.y && pt.y <= y2);
}
inline void Rect2d::Translate(double dx, double dy)
{
	x1 += dx, x2 += dx, y1 += dy, y2 += dy;
}
