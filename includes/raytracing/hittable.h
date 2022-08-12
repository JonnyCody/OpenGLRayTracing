#ifndef RAYTRACING_HITTABLE_H
#define RAYTRACING_HITTABLE_H

#include <memory>

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include "material.h"
#include "aabb.h"

class Hittable
{
public:
    virtual bool BoundingBox(AABB& output_box) const = 0;

    point3 center;
    float radius;
    float x0, x1, y0, y1, z0, z1, k;
    std::shared_ptr<Material> matPtr;
    AABB box;
    int objectType;
};

#endif //RAYTRACING_HITTABLE_H
