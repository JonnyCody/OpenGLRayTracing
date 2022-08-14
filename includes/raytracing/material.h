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
    color(co),materialType(type), roughness(roughness), ior(ior) {}
    vec3 color;
    int materialType;
    float roughness;
    float ior;
    
};

#endif