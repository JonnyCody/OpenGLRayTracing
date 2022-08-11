#ifndef RAY_TRACING_CREATE_SCENE_H_
#define RAY_TRACING_CREATE_SCENE_H_
#include <vector>
#include <random>
#include "sphere.h"

extern const int MAT_LAMBERTIAN, MAT_METALLIC, MAT_DIELECTRIC;

inline double RandomNumber()
{
    static std::uniform_real_distribution<double> distribution(0.0, 1.0);
    static std::mt19937 generator;
    return distribution(generator);
}

inline double RandomNumber(double min, double max)
{
    return min + (max - min) * RandomNumber();
}

vec3 RandomVec3(double min, double max)
{
    return vec3(RandomNumber(min, max), RandomNumber(min, max), RandomNumber(min, max));
}

vec3 RandomVec3()
{
    return vec3(RandomNumber(), RandomNumber(), RandomNumber());
}

void Scene1(std::vector<Sphere>& spheres)
{
    spheres.push_back(Sphere(vec3(0.0, -100.5, -1.0), 100.0, 
    Material(vec3(0.1, 0.7, 0.6), MAT_LAMBERTIAN)));
    spheres.push_back(Sphere(vec3(0.0, 0.0, -1.0), 0.5, 
    Material(vec3(0.5, 0.7, 0.5), MAT_METALLIC)));
    spheres.push_back(Sphere(vec3(-1.0, 0.0, -1.0), 0.5, 
    Material(vec3(0.8, 0.8, 0.0), MAT_LAMBERTIAN)));
    spheres.push_back(Sphere(vec3(1.0, 0.0, -1.0), 0.5, 
    Material(vec3(0.1, 0.8, 0.4), MAT_LAMBERTIAN)));
}

void RandomScene(std::vector<Sphere>& spheres)
{
    
    spheres.push_back(Sphere(vec3(0, -1000, 0), 1000,
    Material(vec3(0.1, 0.7, 0.6), MAT_LAMBERTIAN)));
    for(int a = -11; a < 11; ++a)
    {
        for( int b = -11; b < 11; ++b)
        {
            auto choose_mat = RandomNumber();
            point3 center(a + 0.9*RandomNumber(), 0.2, b + 0.9*RandomNumber());

            if((center - point3(4, 0.2, 0)).length() > 0.9)
            {
                auto albedo = RandomVec3() * RandomVec3();
                spheres.push_back(Sphere(center, 0.2, 
                Material(albedo, MAT_LAMBERTIAN)));
            }
            else if(choose_mat < 0.95)
            {
                auto albedo = RandomVec3(0.5, 1.0);
                auto roughness = RandomNumber(0, 0.5);
                spheres.push_back(Sphere(center, 0.2, 
                Material(albedo, MAT_METALLIC)));
            }
            else
            {
                spheres.push_back(Sphere(center, 0.2, 
                Material(vec3(1.0, 1.0, 1.0), MAT_DIELECTRIC, 0.0, 1.5)));
            }
        }
    }
    spheres.push_back(Sphere(vec3(0, 1, 0), 1.0,
    Material(vec3(1.0, 1.0, 1.0), MAT_DIELECTRIC, 0.0, 1.5)));
    spheres.push_back(Sphere(vec3(-4, 1, 0), 1.0, 
    Material(vec3(0.4, 0.2, 0.1), MAT_LAMBERTIAN)));
    spheres.push_back(Sphere(vec3(4, 1, 0), 1.0,
    Material(vec3(0.7, 0.6, 0.5), MAT_METALLIC)));
}


#endif