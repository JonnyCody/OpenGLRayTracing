#ifndef RAY_TRACING_BVH_H_
#define RAY_TRACING_BVH_H_
#include "aabb.h"

class BVH
{
public:
    BVH(int objectType, int index, int left, int right, const AABB& ab):
    objectType(objectType), objectIndex(index), left(left), right(right),
    ab(ab){}
private:
    int objectType;
    int objectIndex;
    int left, right;
    AABB ab;
};

#endif