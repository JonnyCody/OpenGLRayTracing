#ifndef RAYTRACING_SPHERE_H
#define RAYTRACING_SPHERE_H

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include "hittable.h"

using glm::vec3;

class sphere : public hittable
{
public:
    using point3 = glm::vec3;

    sphere(const vec3 cen, double rad) : center(cen), radius(rad)
    {
        bounding_box(box);
    }

    virtual bool bounding_box(aabb& output_box) const override;

    point3 center;
    double radius;
    aabb box;
};

bool sphere::bounding_box(aabb &output_box) const
{
    output_box = aabb(center - vec3(radius, radius, radius),
                      center + vec3(radius, radius, radius));

    return true;
}

#endif //RAYTRACING_SPHERE_H
