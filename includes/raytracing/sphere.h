#ifndef RAYTRACING_SPHERE_H
#define RAYTRACING_SPHERE_H

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include "hittable.h"
#include "material.h"

extern const int OBJ_SPHERE;

using glm::vec3;

class Sphere : public Hittable
{
public:

    Sphere(const vec3& cen, float rad, std::shared_ptr<Material> m) 
    {
        center = cen;
        radius = rad;
        matPtr = m;
        box = AABB(center - vec3(radius, radius, radius),center + vec3(radius, radius, radius));
        objectType = OBJ_SPHERE;
    } 

    virtual bool BoundingBox(AABB& output_box) const override;
};

bool Sphere::BoundingBox(AABB &output_box) const
{
    output_box = AABB(center - vec3(radius, radius, radius),
                      center + vec3(radius, radius, radius));

    return true;
}

#endif //RAYTRACING_SPHERE_H
