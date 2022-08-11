#ifndef RAY_TRACING_MATERIAL_H_
#define RAY_TRACING_MATERIAL_H_

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

using glm::vec3;

class Material
{
public:
    Material(const vec3& co, int type, float roughness = 0, float ior = 0):
    color(co), roughness(roughness), ior(ior), materialType(type){}
    vec3 color;
    float roughness;
    float ior;
    int materialType;
};

#endif