#ifndef RAYTRACING_AABB_H
#define RAYTRACING_AABB_H

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>



class aabb
{
public:
    using point3 = glm::vec3;
    aabb(const point3& a, const point3& b) {minimum = a; maximum = b;}

    point3 min()  {return minimum;}
    point3 max()  {return maximum;}

    point3 minimum;
    point3 maximum;
};

#endif //RAYTRACING_AABB_H
