#version 330 core
#define PI 3.14159265
const float RAYCAST_MAX = 100000.0;
const int MAT_LAMBERTIAN = 0;
const int MAT_METALLIC =  1;
const int MAT_DIELECTRIC = 2;
const int MAT_PBR =  3;
const int MAT_LIGHT = 4;
// in variables
// ------------
in vec2 screenCoord;

// textures
// --------
uniform vec2 screenSize;

// out variables
// ------------
out vec4 FragColor;

// define struct
// -------------
struct Ray 
{
    vec3 origin;
    vec3 direction;
}; 

struct CameraParameter
{
	vec3 lookFrom;
	vec3 lookAt;
	vec3 vup;
	float vfov;
	float aspectRatio;
};

struct Camera 
{
    vec3 origin;
	vec3 lowerLeftCorner;
	vec3 horizontal;
	vec3 vertical;
	vec3 u, v, w;
	float lensRadius;
}; 

struct Sphere 
{
    vec3 center;
    float radius;
    int materialType;
    int material;
}; 

struct RectangleXY
{
	float x0, x1, y0, y1, k;
	int materialType;
    int material;
};

struct RectangleXZ
{
	float x0, x1, z0, z1, k;
	int materialType;
    int material;
};

struct RectangleYZ
{
	float y0, y1, z0, z1, k;
	int materialType;
    int material;
};

struct AABB
{
	vec3 maximum;
	vec3 minimum;
};

struct BVHNode
{
	AABB aabb;
	int children[2];
	int parent;
	int objectIndex;
};

struct HitRecord
{
	float t;
	vec3 position;
	vec3 normal;

    int materialType;
    int material;
};

struct World
{
    int objectCount;
	int nodesHead;
	RectangleXY rectsXY[40];
	RectangleXZ rectsXZ[40];
	RectangleYZ rectsYZ[40];
    Sphere spheres[40];
	AABB aabbs[40];
	BVHNode nodes[40];
};



struct Lambertian
{
	vec3 albedo;
};

struct Metallic
{
	vec3 albedo;
    float roughness;
};

struct Dielectric
{
	vec3 albedo;
    float roughness;
    float ior;
};

struct DiffuseLight
{
	vec3 emit;
};

// global variables
// ----------------
uniform float rdSeed[4];
int rdCnt = 0;

World world;
Camera camera;
uniform CameraParameter cameraParameter;
Lambertian lambertMaterials[4];
Metallic metallicMaterials[4];
Dielectric dielectricMaterials[4];
DiffuseLight diffuseLight[4];
int stack[10];
int stackTop = -1;

// functions declaration
// ---------------------
float RandXY(float x, float y);
float Rand();
vec2 RandInSquare();
vec3 RandInSphere();
float VecLength(vec3 v);
Ray RayConstructor(vec3 origin, vec3 direction);
vec3 RayGetPointAt(Ray ray, float t);
float RayHitSphere(Ray ray, Sphere sphere);
vec3 SetFaceNormal(Ray ray, vec3 outwardNormal);
Camera CameraConstructor(vec3 lookFrom, vec3 lookAt, vec3 vup, float vfov, float aspectRatio);
Sphere SphereConstructor(vec3 center, float radius, int materialType, int material);
RectangleXY RectangleXYConstructor(float x0, float x1, float y0, float y1, float k, int materialType, int material);
RectangleXZ RectangleXZConstructor(float x0, float x1, float z0, float z1, float k, int materialType, int material);
RectangleYZ RectangleYZConstructor(float y0, float y1, float z0, float z1, float k, int materialType, int material);
bool SphereHit(Sphere sphere, Ray ray, float t_min, float t_max, inout HitRecord hitRec);
bool RectangleXYHit(RectangleXY rect, float tMin, float tMax, inout HitRecord hitRec);
bool RectangleXZHit(RectangleXZ rect, float tMin, float tMax, inout HitRecord hitRec);
bool RectangleYZHit(RectangleYZ rect, float tMin, float tMax, inout HitRecord hitRec);
World WorldConstructor();
World WorldConstructor1();
bool WorldHit(World world, Ray ray, float t_min, float t_max, inout HitRecord rec);
bool WorldHitBVH(World world, Ray ray, float t_min, float t_max, inout HitRecord rec);
vec3 WorldTrace(World world, Ray ray, int depth);
Ray CameraGetRay(Camera camera, vec2 uv);
vec3 GetEnvironmentColor(World world, Ray ray);
Lambertian LambertianConstructor(vec3 albedo);
Metallic MetallicConstructor(vec3 albedo, float roughness);
DiffuseLight DiffuseLightConstructor(vec3 emit);
bool MetallicScatter(in Metallic metallic, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool LambertianScatter(in Lambertian lambertian, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool MaterialScatter(in int materialType, in int material, in Ray incident, in HitRecord hitRecord, out Ray scatter, out vec3 attenuation);
bool LightScatter();
Dielectric DielectricConstructor(vec3 albedo, float roughness, float ior);
bool DielectricScatter1(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool DielectricScatter2(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool DielectricScatter(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
float schlick(float cosine, float ior);
vec3 reflect(in vec3 incident, in vec3 normal);
bool refract(vec3 v, vec3 n, float niOverNt, out vec3 refracted);
bool AABBHit(Ray ray, AABB aabb, float tMin, float tMax);
AABB AABBConstruct(Sphere sphere);
AABB AABBConstructXY(RectangleXY rect);
AABB AABBConstructXZ(RectangleXZ rect);
AABB AABBConstructYZ(RectangleYZ rect);
AABB SurroundingBox(AABB box1, AABB box2);
void SortAABB(inout World world, int start, int end, int axis);
void SortAABBBefore(inout World world);
void BVHNodeConstruct(inout World world);
bool StackEmpty();
int StackTop();
void StackPush(int val);
int StackPop();
bool StackEmpty()
{
	return stackTop == -1;
}

int StackTop()
{
	return stack[stackTop];

}

void StackPush(int val)
{
	++stackTop;
	stack[stackTop] = val;
}

int StackPop()
{
	return stack[stackTop--];
}



void InitScene();
void InitScene1();

// functions definition
// --------------------
float RandXY(float x, float y)
{
    return fract(cos(dot(vec2(x,y), vec2(12.9898, 4.1414))) * 43758.5453);
}
float Rand()
{
    float a = RandXY(screenCoord.x, rdSeed[0]);
    float b = RandXY(rdSeed[1], screenCoord.y);
    float c = RandXY(rdCnt++, rdSeed[2]);
    float d = RandXY(rdSeed[3], a);
    float e = RandXY(b, c);
    float f = RandXY(d, e);

    return f;
}
vec2 RandInSquare()
{
	return vec2(Rand(), Rand());
}

vec3 RandInSphere()
{
    vec3 p;
	
	float theta = Rand() * 2.0 * PI;
	float phi   = Rand() * PI;
	p.y = cos(phi);
	p.x = sin(phi) * cos(theta);
	p.z = sin(phi) * sin(theta);
	
	return p;
}

float VecLength(vec3 v)
{
	return sqrt(v.x*v.x + v.y*v.y + v.z*v.z);
}

Ray RayConstructor(vec3 origin, vec3 direction)
{
	Ray ray;
	ray.origin = origin;
	ray.direction = direction;

	return ray;
}

vec3 RayGetPointAt(Ray ray, float t)
{
	return ray.origin + t * ray.direction;
}

float RayHitSphere(Ray ray, Sphere sphere)
{
	vec3 oc = ray.origin - sphere.center;
	
	float a = dot(ray.direction, ray.direction);
	float b = 2.0 * dot(oc, ray.direction);
	float c = dot(oc, oc) - sphere.radius * sphere.radius;

	float discriminant = b * b - 4 * a * c;
	if(discriminant<0)
		return -1.0;
	else
		return (-b - sqrt(discriminant)) / (2.0 * a);
}

Camera CameraConstructor(vec3 lookFrom, vec3 lookAt, vec3 vup, float vfov, float aspectRatio)
{
	Camera camera;
	float h = tan(radians(vfov)/2);
	float viewPortHeight = 2.0*h;
	float viewPortWidth = aspectRatio * viewPortHeight;

	camera.w = normalize(lookFrom - lookAt);
	camera.u = normalize(cross(vup, camera.w));
	camera.v = cross(camera.w, camera.u);

	camera.origin = lookFrom;
	camera.horizontal = viewPortWidth * camera.u;
	camera.vertical = viewPortHeight * camera.v;
	camera.lowerLeftCorner = camera.origin - camera.horizontal/2 - camera.vertical/2 - camera.w;
	
	return camera;
}

Sphere SphereConstructor(vec3 center, float radius, int materialType, int material)
{
	Sphere sphere;

	sphere.center = center;
	sphere.radius = radius;
    sphere.materialType = materialType;
	sphere.material = material;
	return sphere;
}

RectangleXY RectangleXYConstructor(float x0, float x1, float y0, float y1, float k, int materialType, int material)
{
	RectangleXY rect;
	rect.x0 = x0;
	rect.x1 = x1;
	rect.y0 = y0;
	rect.y1 = y1;
	rect.k = k;
	rect.materialType = materialType;
	rect.material = material;
	return rect;
}

RectangleXZ RectangleXZConstructor(float x0, float x1, float z0, float z1, float k, int materialType, int material)
{
	RectangleXZ rect;
	rect.x0 = x0;
	rect.x1 = x1;
	rect.z0 = z0;
	rect.z1 = z1;
	rect.k = k;
	rect.materialType = materialType;
	rect.material = material;
	return rect;
}

RectangleYZ RectangleYZConstructor(float y0, float y1, float z0, float z1, float k, int materialType, int material)
{
	RectangleYZ rect;
	rect.z0 = z0;
	rect.z1 = z1;
	rect.y0 = y0;
	rect.y1 = y1;
	rect.k = k;
	rect.materialType = materialType;
	rect.material = material;
	return rect;
}

bool SphereHit(Sphere sphere, Ray ray, float t_min, float t_max, inout HitRecord hitRec)
{
	vec3 oc = ray.origin - sphere.center;
	
	float a = dot(ray.direction, ray.direction);
	float b = 2.0 * dot(oc, ray.direction);
	float c = dot(oc, oc) - sphere.radius * sphere.radius;

	float discriminant = b * b - 4 * a * c;

	if(discriminant > 0)
    {
        float temp = (-b - sqrt(discriminant)) / (2.0 * a);
        if(temp < t_max && temp > t_min)
        {
            hitRec.t = temp;
            hitRec.position = RayGetPointAt(ray, hitRec.t);
            hitRec.normal = (hitRec.position - sphere.center)/ sphere.radius;
            hitRec.materialType = sphere.materialType;
            hitRec.material = sphere.material;

            return true;
        }

        temp = (-b + sqrt(discriminant)) / (2.0 * a);
		if(temp < t_max && temp> t_min)
		{
			hitRec.t = temp;
			hitRec.position = RayGetPointAt(ray, hitRec.t);
			hitRec.normal = (hitRec.position - sphere.center) / sphere.radius;
			hitRec.materialType = sphere.materialType;
            hitRec.material = sphere.material;

			return true;
		}

    }
    return false;
}

vec3 SetFaceNormal(Ray ray, vec3 outwardNormal)
{
	vec3 normal;
	normal = dot(ray.direction, outwardNormal) > 0 ? outwardNormal : -outwardNormal;
	return normal;
}

bool RectangleXYHit(RectangleXY rect, Ray ray, float tMin, float tMax, inout HitRecord hitRec)
{
	float t = (rect.k - ray.origin.z)/ray.direction.z;

	if (t < tMin || t > tMax)
	{
		return false;
	}
	float x, y;
	x = ray.origin.x + t * ray.direction.x;
	y = ray.origin.y + t * ray.direction.y;
	if(x < rect.x0 || x > rect.x1 || y < rect.y0 || y > rect.y1)
	{
		return false;
	}
	hitRec.normal = SetFaceNormal(ray, vec3(0.0, 0.0, 1.0));
	hitRec.t = t;
	hitRec.position = RayGetPointAt(ray, hitRec.t);
	hitRec.materialType = rect.materialType;
	hitRec.material = rect.material;
	return true;
}

bool RectangleXZHit(RectangleXZ rect, Ray ray, float tMin, float tMax, inout HitRecord hitRec)
{
	float t = (rect.k - ray.origin.z)/ray.direction.z;

	if (t < tMin || t > tMax)
	{
		return false;
	}
	float x, z;
	x = ray.origin.x + t * ray.direction.x;
	z = ray.origin.z + t * ray.direction.z;
	if(x < rect.x0 || x > rect.x1 || z < rect.z0 || z > rect.z1)
	{
		return false;
	}
	hitRec.normal = SetFaceNormal(ray, vec3(0.0, 1.0, 0.0));
	hitRec.t = t;
	hitRec.position = RayGetPointAt(ray, hitRec.t);
	hitRec.materialType = rect.materialType;
	hitRec.material = rect.material;
	return true;
}

bool RectangleYZHit(RectangleYZ rect, Ray ray, float tMin, float tMax, inout HitRecord hitRec)
{
	float t = (rect.k - ray.origin.y)/ray.direction.y;

	if (t < tMin || t > tMax)
	{
		return false;
	}
	float y, z;
	y = ray.origin.y + t * ray.direction.y;
	z = ray.origin.z + t * ray.direction.z;
	if(y < rect.y0 || y > rect.y1 || z < rect.z0 || z > rect.z1)
	{
		return false;
	}
	hitRec.normal = SetFaceNormal(ray, vec3(1.0, 0.0, 0.0));
	hitRec.t = t;
	hitRec.position = RayGetPointAt(ray, hitRec.t);
	hitRec.materialType = rect.materialType;
	hitRec.material = rect.material;
	return true;
}

World WorldConstructor()
{
	World world;

	world.objectCount = 3;
	int index  = 0;
	// world.spheres[index++] = SphereConstructor(vec3( 0.0, -100.5, -1.0), 100.0, MAT_LAMBERTIAN, 0);
	world.spheres[index++] = SphereConstructor(vec3( 0.0,    0.0, -1.0),   0.5, MAT_METALLIC, 1);
	world.spheres[index++] = SphereConstructor(vec3(-1.0,    0.0, -1.0),   0.5, MAT_DIELECTRIC, 2);
	world.spheres[index++] = SphereConstructor(vec3( 1.0,    0.0, -1.0),   0.5, MAT_LAMBERTIAN, 3);
	
	for(int i = 0; i < world.objectCount;++i)
	{
		world.aabbs[i] = AABBConstruct(world.spheres[i]);
	}
	return world;
}

World WorldConstructor1()
{
	World world;
	world.objectCount = 6;
	world.rectsYZ[0] = RectangleYZConstructor(0.0, 555.0, 0.0, 555.0, 555.0, MAT_LAMBERTIAN, 2);
	world.rectsYZ[1] = RectangleYZConstructor(0.0, 555.0, 0.0, 555.0,   0.0, MAT_LAMBERTIAN, 0);
	world.rectsXZ[0] = RectangleXZConstructor(213.0, 343.0, 227.0, 332.0, 554.0, MAT_LIGHT, 0);
	world.rectsXZ[1] = RectangleXZConstructor(0.0, 555.0, 0.0, 555.0, 0.0, MAT_LAMBERTIAN, 1);
	world.rectsXZ[2] = RectangleXZConstructor(0.0, 555.0, 0.0, 555.0, 555.0, MAT_LAMBERTIAN, 1);
	world.rectsXY[0] = RectangleXYConstructor(0.0, 555.0, 0.0, 555.0, 555.0, MAT_LAMBERTIAN, 0);
	return world;
}

bool WorldHit(World world, Ray ray, float t_min, float t_max, inout HitRecord rec)
{
    HitRecord tmpRec;
    float cloestSoFar = t_max;
    bool hitSomething = false;

    for(int i = 0; i < world.objectCount; ++i)
    {
        // if(SphereHit(world.spheres[i], ray, t_min, cloestSoFar, tmpRec))
        // {
        //     rec = tmpRec;
        //     cloestSoFar = tmpRec.t;

        //     hitSomething = true;
        // }
		if(RectangleXYHit(world.rectsXY[i], ray,  t_min,  cloestSoFar, tmpRec))
		{
			rec = tmpRec;
            cloestSoFar = tmpRec.t;

            hitSomething = true;
		}
		else if(RectangleXZHit(world.rectsXZ[i], ray,  t_min,  cloestSoFar, tmpRec))
		{
			rec = tmpRec;
            cloestSoFar = tmpRec.t;

            hitSomething = true;
		}
		else if(RectangleYZHit(world.rectsYZ[i], ray,  t_min,  cloestSoFar, tmpRec))
		{
			rec = tmpRec;
            cloestSoFar = tmpRec.t;

            hitSomething = true;
		}
    }
    return hitSomething;
}

bool WorldHitBVH(World world, Ray ray, float t_min, float t_max, inout HitRecord rec)
{
    HitRecord tmpRec;
    float cloestSoFar = t_max;
    bool hitSomething = false;
	int curr = world.nodesHead;
	while(curr != -1 || !StackEmpty())
	{
		if(AABBHit(ray, world.nodes[curr].aabb,t_min, cloestSoFar))
		{
			if(world.nodes[curr].objectIndex != -1)
			{
				if(SphereHit(world.spheres[world.nodes[curr].objectIndex],ray, t_min,cloestSoFar,tmpRec))
				{
					rec = tmpRec;
					cloestSoFar = tmpRec.t;
            		hitSomething = true;
				}
				if(StackEmpty())
				{
					curr = -1;
				}
				else
				{
					curr = StackPop();
				}
			}
			else
			{
				StackPush(world.nodes[curr].children[1]);
				curr = world.nodes[curr].children[0];
			}
		}
		else
		{
			if(StackEmpty())
			{
				curr = -1;
			}
			else
			{
				curr = StackPop();
			}
		}
	}
    return hitSomething;
}

vec3 WorldTrace(World world, Ray ray, int depth)
{
    HitRecord hitRecord;

	vec3 frac = vec3(1.0, 1.0, 1.0);
	vec3 bgColor = vec3(0.0, 0.0, 0.0);
	while(depth>0)
	{
		depth--;
		if(WorldHit(world, ray, 0.001, RAYCAST_MAX, hitRecord))
		// if(WorldHitBVH(world, ray, 0.001, RAYCAST_MAX, hitRecord))
		{
			Ray scatterRay;
			vec3 attenuation;
			if(!MaterialScatter(hitRecord.materialType, hitRecord.material, ray, hitRecord, scatterRay, attenuation))
			{
				if(hitRecord.materialType == MAT_LIGHT)
				{
					return diffuseLight[hitRecord.material].emit;
				}
				else
				{
					break;
				}
			}
			
			frac *= attenuation;
			ray = scatterRay;
		}
		else
		{
			bgColor = GetEnvironmentColor(world, ray);
			break;
		}
	}

	return bgColor * frac;
}

Ray CameraGetRay(Camera camera, vec2 uv)
{
	return RayConstructor(camera.origin, 
		camera.lowerLeftCorner + uv.x * camera.horizontal + uv.y*camera.vertical - 
		camera.origin);
}

vec3 GetEnvironmentColor(World world, Ray ray)
{
    vec3 unit_direction = normalize(ray.direction);
    float t = 0.5 * (unit_direction.y + 1.0);
    return vec3(1.0, 1.0, 1.0) * (1.0 - t) + vec3(0.5, 0.7, 1.0) * t;

    // vec3 dir = normalize(ray.direction);
	// float theta = acos(dir.y) / PI;
	// float phi = (atan(dir.x, dir.z) / (PI) + 1.0) / 2.0;
	// return texture(envMap, vec2(phi, theta)).xyz;
}

Lambertian LambertianConstructor(vec3 albedo)
{
	Lambertian lambertian;

	lambertian.albedo = albedo;

	return lambertian;
}

bool LambertianScatter(in Lambertian lambertian, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = lambertian.albedo;

	scattered.origin = hitRecord.position;
	scattered.direction = hitRecord.normal + RandInSphere();

	return true;
}

Metallic MetallicConstructor(vec3 albedo, float roughness)
{
	Metallic metallic;

	metallic.albedo = albedo;
	metallic.roughness = roughness;

	return metallic;
}

bool MetallicScatter(in Metallic metallic, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = metallic.albedo;

	scattered.origin = hitRecord.position;
	scattered.direction = reflect(incident.direction, hitRecord.normal);

	return dot(scattered.direction, hitRecord.normal) > 0.0;
}

DiffuseLight DiffuseLightConstructor(vec3 emit)
{
	DiffuseLight light;
	light.emit = emit;
	return light;
}

Dielectric DielectricConstructor(vec3 albedo, float roughness, float ior)
{
	Dielectric dielectric;

	dielectric.albedo = albedo;
	dielectric.roughness = roughness;
	dielectric.ior = ior;

	return dielectric;
}

bool DielectricScatter1(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = dielectric.albedo;
	vec3 reflected = reflect(incident.direction, hitRecord.normal);

	vec3 outward_normal;
	float ni_over_nt;
	if(dot(incident.direction, hitRecord.normal) > 0.0)// hit from inside
	{
		outward_normal = -hitRecord.normal;
		ni_over_nt = dielectric.ior;
	}
	else // hit from outside
	{
		outward_normal = hitRecord.normal;
		ni_over_nt = 1.0 / dielectric.ior;
	}

	vec3 refracted;
	if(refract(incident.direction, outward_normal, ni_over_nt, refracted))
	{
		scattered = Ray(hitRecord.position, refracted);

		return true;
	}
	else
	{
		scattered = Ray(hitRecord.position, reflected);

		return false;
	}
}

bool DielectricScatter2(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = dielectric.albedo;
	vec3 reflected = reflect(incident.direction, hitRecord.normal);

	vec3 outward_normal;
	float ni_over_nt;
	float cosine;
	if(dot(incident.direction, hitRecord.normal) > 0.0)// hit from inside
	{
		outward_normal = -hitRecord.normal;
		ni_over_nt = dielectric.ior;
		cosine = dot(incident.direction, hitRecord.normal) / length(incident.direction); // incident angle
	}
	else // hit from outside
	{
		outward_normal = hitRecord.normal;
		ni_over_nt = 1.0 / dielectric.ior;
		cosine = -dot(incident.direction, hitRecord.normal) / length(incident.direction); // incident angle
	}

	float reflect_prob;
	vec3 refracted;
	if(refract(incident.direction, outward_normal, ni_over_nt, refracted))
	{
		reflect_prob = schlick(cosine, dielectric.ior);
	}
	else
	{
		reflect_prob = 1.0;
	}

	if(Rand() < reflect_prob)
	{
		scattered = Ray(hitRecord.position, refracted);
	}
	else
	{
		scattered = Ray(hitRecord.position, refracted);
	}

	return true;
}

bool DielectricScatter(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	//return DielectricScatter1(dielectric, incident, hitRecord, scattered, attenuation);
	return DielectricScatter2(dielectric, incident, hitRecord, scattered, attenuation);
}

float schlick(float cosine, float ior)
{
	float r0 = (1 - ior) / (1 + ior);
	r0 = r0 * r0;
	return r0 + (1 - r0) * pow((1 - cosine), 5);
}

vec3 reflect(in vec3 incident, in vec3 normal)
{
	return incident - 2 * dot(normal, incident) * normal;
}

bool refract(vec3 v, vec3 n, float ni_over_nt, out vec3 refracted)
{
	vec3 uv = normalize(v);
	float dt = dot(uv, n);
	float discriminant = 1.0 - ni_over_nt * ni_over_nt * (1.0 - dt * dt);
	if (discriminant > 0)
	{
		refracted = ni_over_nt * (uv - n * dt) - n * sqrt(discriminant);
		return true;
	}
	else
		return false;
}

bool MaterialScatter(in int materialType, in int material, in Ray incident, in HitRecord hitRecord, out Ray scatter, out vec3 attenuation)
{
    if(materialType==MAT_LAMBERTIAN)
		return LambertianScatter(lambertMaterials[material], incident, hitRecord, scatter, attenuation);
	else if(materialType==MAT_METALLIC)
		return MetallicScatter(metallicMaterials[material], incident, hitRecord, scatter, attenuation);
	else if(materialType==MAT_DIELECTRIC)
		return DielectricScatter(dielectricMaterials[material], incident, hitRecord, scatter, attenuation);
	else
		return false;
}

bool AABBHit(Ray ray, AABB aabb, float tMin, float tMax)
{
	for(int a = 0; a < 3; ++a)
	{
		float invD = 1.0f/ray.direction[a];
		float t0 = (aabb.minimum[a]-ray.origin[a]) * invD;
		float t1 = (aabb.maximum[a]-ray.origin[a]) * invD;

		if(invD < 0.0f)
		{
			float tmp = t0;
			t0 = t1;
			t1 = tmp;
		}
		tMin = t0 > tMin ? t0 : tMin;
		tMax = t1 < tMax ? t1 : tMax;
		if(tMax <= tMin)
			return false;
	}
	return true;
}

AABB AABBConstruct(Sphere sphere)
{
	AABB aabb;
	aabb.maximum = sphere.center + vec3(sphere.radius, sphere.radius, sphere.radius);
	aabb.minimum = sphere.center - vec3(sphere.radius, sphere.radius, sphere.radius);
	return aabb;
}

AABB AABBConstructXY(RectangleXY rect)
{
	AABB aabb;
	aabb.minimum = vec3(rect.x0, rect.y0, rect.k-0.0001);
	aabb.maximum = vec3(rect.x1, rect.y1, rect.k+0.0001);
	return aabb;
}
AABB AABBConstructXZ(RectangleXZ rect)
{
	AABB aabb;
	aabb.minimum = vec3(rect.x0, rect.k-0.0001, rect.z0);
	aabb.maximum = vec3(rect.x1, rect.k+0.0001, rect.z1);
	return aabb;
}
AABB AABBConstructYZ(RectangleYZ rect)
{
	AABB aabb;
	aabb.minimum = vec3(rect.k - 0.0001, rect.y0, rect.z0);
	aabb.maximum = vec3(rect.k + 0.0001, rect.y1, rect.z1);
	return aabb;
}

AABB SurroundingBox(AABB box1, AABB box2)
{
	AABB ab;
	ab.minimum = vec3(min(box1.minimum[0], box2.minimum[0]),
					min(box1.minimum[1], box2.minimum[1]),
					min(box1.minimum[2], box2.minimum[2]));
	ab.maximum = vec3(max(box1.maximum[0], box2.maximum[0]),
					max(box1.maximum[1], box2.maximum[1]),
					max(box1.maximum[2], box2.maximum[2]));
	return ab;
}

void SortAABB(inout World world, int start, int end, int axis)
{
	AABB aabb;
	Sphere sphere;
	if(end > world.objectCount)
	{
		end = world.objectCount;
	}
	for(int i = start; i < end - 1 ; ++i)
	{
		for(int j = start; j < end - i + start -1; ++j)
		{
			if(world.aabbs[j].minimum[axis] > world.aabbs[j+1].minimum[axis])
			{
				aabb = world.aabbs[j];
				world.aabbs[j] = world.aabbs[j+1];
				world.aabbs[j+1] = aabb;

				sphere = world.spheres[j];
				world.spheres[j] = world.spheres[j+1];
				world.spheres[j+1] = sphere;
			}
		}
	}
}

void SortAABBBefore(inout World world)
{
	int span = world.objectCount;
	int start = 0;
	int axis = 0;
	while(span >= 2)
	{
		while(start < world.objectCount - 1)
		{
			axis = int(Rand()*3)%3;
			SortAABB(world, start, start + span, axis);
			start += span;
		}
		span/=2;
	}
}

void BVHNodeConstruct(inout World world)
{
	SortAABBBefore(world);
	world.nodesHead = world.objectCount * 2 - 2;
	int parent = world.objectCount;
	for(int i = 0; parent <= world.nodesHead; i += 2)
	{
		if(i < world.objectCount)
		{
			world.nodes[i].aabb = world.aabbs[i];
			world.nodes[i].children[0] = world.nodes[i].children[1] = -1;
			world.nodes[i].parent = parent;
			world.nodes[i].objectIndex = i;
			if((i+1) < world.objectCount)
			{
				world.nodes[i + 1].aabb = world.aabbs[i + 1];
				world.nodes[i + 1].children[0] = world.nodes[i+1].children[1] = -1;
				world.nodes[i + 1].parent = parent;
				world.nodes[i + 1].objectIndex = i + 1;
			}
			else
			{
				world.nodes[i + 1].parent = parent;
			}
		}
		else
		{
			world.nodes[i].parent = parent;
			world.nodes[i + 1].parent = parent;
		}
		world.nodes[parent].aabb = SurroundingBox(world.nodes[i].aabb, world.nodes[i + 1].aabb);
		world.nodes[parent].children[0] = i;
		world.nodes[parent].children[1] = i + 1;
		world.nodes[parent].objectIndex = -1;
		++parent;
	}
}



void InitScene()
{
	vec3 lookFrom = vec3(-5.0, 4.0, 4.0);
	vec3 lookAt = vec3(-4.6, 3.5, 3.4);
	vec3 vup = vec3(0.0, 1.0, 0.0);
	world = WorldConstructor();
	camera = CameraConstructor(cameraParameter.lookFrom, cameraParameter.lookAt, cameraParameter.vup, 20.0, cameraParameter.aspectRatio);
	// camera = CameraConstructor(lookFrom, lookAt, vup, 20.0, float(4.0/3.0));
	lambertMaterials[0] = LambertianConstructor(vec3(0.5, 0.5, 0.5));
	lambertMaterials[1] = LambertianConstructor(vec3(0.4, 0.2, 0.1));
	lambertMaterials[2] = LambertianConstructor(vec3(0.5, 0.5, 0.7));
	lambertMaterials[3] = LambertianConstructor(vec3(0.8, 0.8, 0.0));

	metallicMaterials[0] = MetallicConstructor(vec3(0.7, 0.6, 0.5), 0.0);
	metallicMaterials[1] = MetallicConstructor(vec3(0.5, 0.7, 0.5), 0.1);
	metallicMaterials[2] = MetallicConstructor(vec3(0.5, 0.5, 0.7), 0.2);
	metallicMaterials[3] = MetallicConstructor(vec3(0.7, 0.7, 0.7), 0.3);

	dielectricMaterials[0] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.0, 1.5);
	dielectricMaterials[1] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.1, 1.5);
	dielectricMaterials[2] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.2, 1.5);
	dielectricMaterials[3] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.3, 1.5);
}

void InitScene1()
{
	world = WorldConstructor1();
	vec3 lookAt = vec3(278.0, 278.0, 0.0);
	camera = CameraConstructor(cameraParameter.lookFrom, cameraParameter.lookAt, cameraParameter.vup, 40.0, cameraParameter.aspectRatio);
	// red
	lambertMaterials[0] = LambertianConstructor(vec3(0.65, 0.05, 0.05));
	// white
	lambertMaterials[1] = LambertianConstructor(vec3(0.73, 0.73, 0.73));
	// green
	lambertMaterials[2] = LambertianConstructor(vec3(0.12, 0.45, 0.15));
	// light
	diffuseLight[0] = DiffuseLightConstructor(vec3(15.0, 15.0, 15.0));

}

// main function
// -------------
void main()
{
	InitScene1();
	// BVHNodeConstruct(world);
	vec3 col = vec3(0.0, 0.0, 0.0);
	int ns = 50;
	for(int i=0; i<ns; i++)
	{
		Ray ray = CameraGetRay(camera, screenCoord + RandInSquare() / screenSize);
		col += WorldTrace(world, ray, 50);
	}
	col /= ns;

	//col = GammaCorrection(col);

	FragColor.xyz = col;
	FragColor.w = 1.0;
}