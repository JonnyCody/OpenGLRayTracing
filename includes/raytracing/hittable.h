#ifndef RAYTRACING_HITTABLE_H
#define RAYTRACING_HITTABLE_H

#include <memory>

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include "aabb.h"

class Hittable
{
public:
    virtual bool bounding_box(AABB& output_box) const = 0;
};

#endif //RAYTRACING_HITTABLE_H
