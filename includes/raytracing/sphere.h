#ifndef RAYTRACING_SPHERE_H
#define RAYTRACING_SPHERE_H

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include "hittable.h"
#include "material.h"

using glm::vec3;

class Sphere : public Hittable
{
public:

    Sphere(const vec3& cen, float rad, Material material) : center(cen), radius(rad), material(material), 
    box(center - vec3(radius, radius, radius),center + vec3(radius, radius, radius)){}

    virtual bool bounding_box(AABB& output_box) const override;

    point3 center;
    float radius;
    Material material;
    AABB box;
};

bool Sphere::bounding_box(AABB &output_box) const
{
    output_box = AABB(center - vec3(radius, radius, radius),
                      center + vec3(radius, radius, radius));

    return true;
}

#endif //RAYTRACING_SPHERE_H
