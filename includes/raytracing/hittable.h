#ifndef RAYTRACING_HITTABLE_H
#define RAYTRACING_HITTABLE_H

#include <memory>

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include "aabb.h"

class hittable
{
public:
    virtual bool bounding_box(aabb& output_box) const = 0;
};

#endif //RAYTRACING_HITTABLE_H
