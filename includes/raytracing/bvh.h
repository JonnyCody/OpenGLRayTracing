#ifndef RAY_TRACING_BVH_H_
#define RAY_TRACING_BVH_H_
#include <vector>

#include "aabb.h"
#include "hittable_list.h"
#include "sphere.h"

using std::vector;

class BVHNode
{
public:
    BVHNode()
    {
        objectIndex = -1;
        objectType  =-1;
        left = right = parent = -1;
        aabb.maximum = vec3(0, 0, 0);
        aabb.minimum = vec3(0, 0, 0);
    }
    BVHNode(int objectType, int index, int left, int right, const AABB& ab):
    objectType(objectType), objectIndex(index), left(left), right(right), aabb(ab)
    {
        
    }

    int objectType;
    int objectIndex;
    int left, right, parent;
    AABB aabb;
};

void BuildBVHNodes(vector<BVHNode>& BVHNodes, HittableList& objects)
{
    int nodesHead = objects.size()*2-2;
    int parent = objects.size();
    for(int i = 0; parent <= nodesHead; i += 2)
    {
        if( i < objects.size())
        {
            objects[i]->BoundingBox(BVHNodes[i].aabb);
            BVHNodes[i].left = BVHNodes[i].right = -1;
            BVHNodes[i].parent = parent;
            BVHNodes[i].objectIndex = i;
            BVHNodes[i].objectType = objects[i]->objectType;
            if((i + 1) < objects.size())
            {
                objects[i + 1]->BoundingBox(BVHNodes[i + 1].aabb);
                BVHNodes[i + 1].left = BVHNodes[i].right = -1;
                BVHNodes[i + 1].parent = parent;
                BVHNodes[i + 1].objectIndex = i + 1;
                BVHNodes[i + 1].objectType = objects[i + 1]->objectType;
            }
            else
            {
                BVHNodes[i + 1].parent = parent;
            }
        }
        else
        {
            BVHNodes[i].parent = parent;
            BVHNodes[i + 1].parent = parent;
        }
        BVHNodes[parent].aabb = SurroundingBox(BVHNodes[i].aabb, 
                            BVHNodes[i + 1].aabb);
        BVHNodes[parent].left = i;
        BVHNodes[parent].right = i + 1;
        BVHNodes[parent].objectIndex = -1;
        BVHNodes[parent].objectType = -1;
        ++parent;
    }
}

#endif