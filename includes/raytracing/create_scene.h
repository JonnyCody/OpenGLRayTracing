#ifndef RAY_TRACING_CREATE_SCENE_H_
#define RAY_TRACING_CREATE_SCENE_H_
#include <vector>
#include "sphere.h"

extern const int MAT_LAMBERTIAN, MAT_METALLIC, MAT_LAMBERTIAN, MAT_LAMBERTIAN;

std::vector<Sphere> Scene1()
{
    std::vector<Sphere> spheres;
    spheres.push_back(Sphere(vec3(0.0, -100.5, -1.0), 100.0, 
    Material(vec3(0.1, 0.7, 0.6), MAT_LAMBERTIAN)));
    spheres.push_back(Sphere(vec3(0.0, 0.0, -1.0), 0.5, 
    Material(vec3(0.5, 0.7, 0.5), MAT_METALLIC)));
    spheres.push_back(Sphere(vec3(-1.0, 0.0, -1.0), 0.5, 
    Material(vec3(0.8, 0.8, 0.0), MAT_LAMBERTIAN)));
    spheres.push_back(Sphere(vec3(1.0, 0.0, -1.0), 0.5, 
    Material(vec3(0.1, 0.8, 0.4), MAT_LAMBERTIAN)));
    return spheres;
}


#endif