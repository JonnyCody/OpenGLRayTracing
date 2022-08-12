#ifndef RAY_TRACING_HITTABLE_LIST_H_
#define RAY_TRACING_HITTABLE_LIST_H_

#include <vector>

#include "hittable.h"

class HittableList : public Hittable
{
public:
    HittableList(){}
    void clear()
    {
        objects.clear();
    }
    void add(std::shared_ptr<Hittable> object)
    {
        objects.push_back(object);
    }

    virtual bool BoundingBox(AABB& aabb) const override
    {
        if(objects.empty())
        {
            return false;
        }
        bool firstBox = true;
        AABB tmpBox;
        for(auto object : objects)
        {
            if(!object->BoundingBox(tmpBox))
            {
                return false;
            }
            aabb = firstBox ? tmpBox : SurroundingBox(aabb, tmpBox);
            firstBox = false;
        }
        return true;
    }
    std::vector<std::shared_ptr<Hittable>>::iterator begin()
    {
        return objects.begin();
    }
    std::vector<std::shared_ptr<Hittable>>::iterator end()
    {
        return objects.end();
    }
    std::shared_ptr<Hittable> operator[](unsigned int i)
    {
        return objects[i];
    }
    auto size()
    {
        return objects.size();
    }
    auto& get_object() const
    {
        return objects;
    }

    std::vector<std::shared_ptr<Hittable>> objects;
};

#endif