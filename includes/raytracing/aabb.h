#ifndef RAYTRACING_AABB_H
#define RAYTRACING_AABB_H

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

using point3 = glm::vec3;

class AABB
{
public:
    
    AABB(const point3& a, const point3& b) {minimum = a; maximum = b;}

    point3 min()  {return minimum;}
    point3 max()  {return maximum;}
private:
    point3 minimum;
    point3 maximum;

};

AABB surrounding_box(AABB box0, AABB box1)
{
    point3 small(fmin(box0.min()[0], box1.min()[0]),
                 fmin(box0.min()[1], box1.min()[1]),
                 fmin(box0.min()[2], box1.min()[2]));

    point3 big(fmax(box0.max()[0], box1.max()[0]),
               fmax(box0.max()[1], box1.max()[1]),
               fmax(box0.max()[2], box1.max()[2]));

    return AABB(small, big);
}

#endif //RAYTRACING_AABB_H
