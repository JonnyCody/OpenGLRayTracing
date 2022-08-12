#ifndef RAY_TRACING_RECTANGLE_H_
#define RAY_TRACING_RECTANGLE_H_
#include "hittable.h"
#include "material.h"

extern const int OBJ_XYRECT, OBJ_XZRECT, OBJ_YZRECT;

class XYRect : public Hittable
{
public:
    XYRect(float _x0, float _x1, float _y0, float _y1, float _k, std::shared_ptr<Material> m)
    {
        objectType = OBJ_XYRECT;
        x0 = _x0;
        x1 = _x1;
        y0 = _y0;
        y1 = _y1; 
        k = _k; 
        matPtr = m;
        box = AABB(point3(x0, y0, k - 0.0001), point3(x1, y1, k + 0.0001));
    }

    virtual bool BoundingBox(AABB& aabb) const override
    {
        aabb = AABB(point3(x0, y0, k - 0.0001), point3(x1, y1, k + 0.0001));
        return true;
    }
};

class XZRect : public Hittable
{
public:
    XZRect(float _x0, float _x1, float _z0, float _z1, float _k, std::shared_ptr<Material> m)
    {
        objectType = OBJ_XZRECT;
        x0 = _x0;
        x1 = _x1;
        z0 = _z0;
        z1 = _z1;
        k = _k;
        matPtr = m;
        box = AABB(point3(x0, k - 0.0001, z0), point3(x1, k + 0.0001, z1));
    }
    
    virtual bool BoundingBox(AABB& aabb) const override
    {
        aabb = AABB(point3(x0, k - 0.0001, z0), point3(x1, k + 0.0001, z1));
        return true;
    }
};

class YZRect : public Hittable
{
public:
    YZRect(float _y0, float _y1, float _z0, float _z1, float _k, std::shared_ptr<Material> m)
    {
        objectType = OBJ_YZRECT;
        y0 = _y0;
        y1 = _y1;
        z0 = _z0;
        z1 = _z1;
        k = _k;
        matPtr = m;
        box = AABB(point3(k - 0.0001, y0, z0), point3(k + 0.0001, y1, z1));
    }
    
    virtual bool BoundingBox(AABB& aabb) const override
    {
        aabb = AABB(point3(k - 0.0001, y0, z0), point3(k + 0.0001, y1, z1));
        return true;
    }
};

#endif