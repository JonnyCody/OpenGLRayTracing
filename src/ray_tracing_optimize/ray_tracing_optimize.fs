#version 330 core

#define PI 3.14159265
const float RAYCAST_MAX = 100000.0;
const int MAT_LAMBERTIAN = 0;
const int MAT_METALLIC =  1;
const int MAT_DIELECTRIC = 2;
const int MAT_PBR =  3;

const int OBJ_SPHERE = 1;
const int OBJ_XYRECT = 2;
const int OBJ_XZRECT = 3;
const int OBJ_YZRECT = 4;
// in variables
// ------------
in vec2 screenCoord;

// textures
// --------
uniform vec2 screenSize;
uniform samplerBuffer spheresData;
uniform samplerBuffer BVHNodesData;

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

struct Material
{
	int materialType;
	vec3 color;
	float roughness;
	float ior;
};

struct Sphere 
{
    vec3 center;
    float radius;
    Material material;
}; 

struct XYRect
{
	float x0, x1, y0, y1, k;
    Material material;
};

struct XZRect
{
	float x0, x1, z0, z1, k;
    Material material;
};

struct YZRect
{
	float y0, y1, z0, z1, k;
    Material material;
};

struct AABB
{
	vec3 maximum;
	vec3 minimum;
};

struct BVHNode
{
	AABB aabb;
	int left, right;
	int parent;
	int objectIndex;
	int objectType;
};

struct HitRecord
{
	float t;
	vec3 position;
	vec3 normal;

    Material material;
};

struct World
{
    int objectCount;
	int nodesHead;
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



// global variables
// ----------------
float rdSeed[4];
int rdCnt = 0;
Camera camera;
uniform CameraParameter cameraParameter;
uniform World world;
int stack[30];
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
Camera CameraConstructor(vec3 lookFrom, vec3 lookAt, vec3 vup, float vfov, float aspectRatio);
Sphere GetSphereFromTexture(int sphereIndex);
XYRect GetXYRectFromTexture(int xyrectIndex);
XZRect GetXZRectFromTexture(int xzrectIndex);
YZRect GetYZRectFromTexture(int yzrectIndex);
BVHNode GetBVHNodeFromTexture(int BVHNodeIndex);
bool SphereHit(Sphere sphere, Ray ray, float tMin, float tMax, inout HitRecord hitRec);
vec3 SetFaceNormal(Ray ray, vec3 outwardNormal);
bool XYRectHit(XYRect rect, Ray ray, float tMin, float tMax, inout HitRecord hitRec);
bool XZRectHit(XZRect rect, Ray ray, float tMin, float tMax, inout HitRecord hitRec);
bool YZRectHit(YZRect rect, Ray ray, float tMin, float tMax, inout HitRecord hitRec);
bool WorldHit(World world, Ray ray, float tMin, float tMax, inout HitRecord rec);
bool WorldHitBVH(Ray ray, float tMin, float tMax, inout HitRecord rec);
vec3 WorldTrace(Ray ray, int depth);
Ray CameraGetRay(Camera camera, vec2 uv);
vec3 GetEnvironmentColor(World world, Ray ray);
Lambertian LambertianConstructor(vec3 albedo);
Metallic MetallicConstructor(vec3 albedo, float roughness);
bool MetallicScatter(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool LambertianScatter(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool MaterialScatter(in Ray incident, in HitRecord hitRecord, out Ray scatter, out vec3 attenuation);
Dielectric DielectricConstructor(vec3 albedo, float roughness, float ior);
bool DielectricScatter1(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool DielectricScatter2(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool DielectricScatter(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
float schlick(float cosine, float ior);
vec3 reflect(in vec3 incident, in vec3 normal);
bool refract(vec3 v, vec3 n, float niOverNt, out vec3 refracted);
bool AABBHit(Ray ray, AABB aabb, float tMin, float tMax);

bool StackEmpty();
int StackTop();
void StackPush(int val);
int StackPop();


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

Sphere GetSphereFromTexture(int sphereIndex)
{
	Material tmpMatrial;
	Sphere sphere;
	vec4 pack;
	int index = sphereIndex*3;
	pack = texelFetch(spheresData, index);
	sphere.center = pack.xyz;
	sphere.radius = pack.w;
	pack = texelFetch(spheresData, index + 1);
	tmpMatrial.color = pack.xyz;
	tmpMatrial.materialType = int(pack.w);
	pack = texelFetch(spheresData, index + 2);
	tmpMatrial.roughness = pack.x;
	tmpMatrial.ior = pack.y;
	sphere.material = tmpMatrial;
	return sphere;
}

XYRect GetXYRectFromTexture(int xyrectIndex)
{
	XYRect rect;
	Material tmpMaterial;
	vec4 pack;
	int index = xyrectIndex * 3;
	pack = texelFetch(spheresData, index);
	rect.x0 = pack.x;
	rect.x1 = pack.y;
	rect.y0 = pack.z;
	rect.y1 = pack.w;
	pack = texelFetch(spheresData, index + 1);
	tmpMaterial.color = pack.xyz;
	rect.k = pack.w;
	pack = texelFetch(spheresData, index + 2);
	tmpMaterial.materialType = int(pack.x);
	tmpMaterial.roughness = pack.y;
	tmpMaterial.ior = pack.z;
	rect.material = tmpMaterial;
	return rect;
}

XZRect GetXZRectFromTexture(int xzrectIndex)
{
	XZRect rect;
	Material tmpMaterial;
	vec4 pack;
	int index = xzrectIndex * 3;
	pack = texelFetch(spheresData, index);
	rect.x0 = pack.x;
	rect.x1 = pack.y;
	rect.z0 = pack.z;
	rect.z1 = pack.w;
	pack = texelFetch(spheresData, index + 1);
	tmpMaterial.color = pack.xyz;
	rect.k = pack.w;
	pack = texelFetch(spheresData, index + 2);
	tmpMaterial.materialType = int(pack.x);
	tmpMaterial.roughness = pack.y;
	tmpMaterial.ior = pack.z;
	rect.material = tmpMaterial;
	return rect;
}

YZRect GetYZRectFromTexture(int yzrectIndex)
{
	YZRect rect;
	Material tmpMaterial;
	vec4 pack;
	int index = yzrectIndex * 3;
	pack = texelFetch(spheresData, index);
	rect.y0 = pack.x;
	rect.y1 = pack.y;
	rect.z0 = pack.z;
	rect.z1 = pack.w;
	pack = texelFetch(spheresData, index + 1);
	tmpMaterial.color = pack.xyz;
	rect.k = pack.w;
	pack = texelFetch(spheresData, index + 2);
	tmpMaterial.materialType = int(pack.x);
	tmpMaterial.roughness = pack.y;
	tmpMaterial.ior = pack.z;
	rect.material = tmpMaterial;
	return rect;
}

BVHNode GetBVHNodeFromTexture(int BVHNodeIndex)
{
	vec4 pack;
	BVHNode node;
	int index = BVHNodeIndex * 3;
	pack = texelFetch(BVHNodesData, index);
	node.aabb.minimum = pack.xyz;
	node.objectIndex = int(pack.w);
	pack = texelFetch(BVHNodesData, index + 1);
	node.aabb.maximum = pack.xyz;
	node.objectType = int(pack.w);
	pack = texelFetch(BVHNodesData, index + 2);
	node.left = int(pack.x);
	node.right = int(pack.y);
	return node;
}

bool SphereHit(Sphere sphere, Ray ray, float tMin, float tMax, inout HitRecord hitRec)
{
	vec3 oc = ray.origin - sphere.center;
	
	float a = dot(ray.direction, ray.direction);
	float b = 2.0 * dot(oc, ray.direction);
	float c = dot(oc, oc) - sphere.radius * sphere.radius;

	float discriminant = b * b - 4 * a * c;

	if(discriminant > 0)
    {
        float temp = (-b - sqrt(discriminant)) / (2.0 * a);
        if(temp < tMax && temp > tMin)
        {
            hitRec.t = temp;
            hitRec.position = RayGetPointAt(ray, hitRec.t);
            hitRec.normal = (hitRec.position - sphere.center)/ sphere.radius;
            hitRec.material = sphere.material;

            return true;
        }

        temp = (-b + sqrt(discriminant)) / (2.0 * a);
		if(temp < tMax && temp> tMin)
		{
			hitRec.t = temp;
			hitRec.position = RayGetPointAt(ray, hitRec.t);
			hitRec.normal = (hitRec.position - sphere.center) / sphere.radius;
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

bool XYRectHit(XYRect rect, Ray ray, float tMin, float tMax, inout HitRecord hitRec)
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
	hitRec.material = rect.material;
	return true;
}

bool XZRectHit(XZRect rect, Ray ray, float tMin, float tMax, inout HitRecord hitRec)
{
	float t = (rect.k - ray.origin.y)/ray.direction.y;

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
	hitRec.material = rect.material;
	return true;
}

bool YZRectHit(YZRect rect, Ray ray, float tMin, float tMax, inout HitRecord hitRec)
{
	float t = (rect.k - ray.origin.x)/ray.direction.x;

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
	hitRec.material = rect.material;
	return true;
}

bool WorldHit(Ray ray, float tMin, float tMax, inout HitRecord rec)
{
    HitRecord tmpRec;
    float cloestSoFar = tMax;
    bool hitSomething = false;

    for(int i = 0; i < world.objectCount; ++i)
    {
        if(SphereHit(GetSphereFromTexture(i), ray, tMin, cloestSoFar, tmpRec))
        {
            rec = tmpRec;
            cloestSoFar = tmpRec.t;

            hitSomething = true;
        }
    }
    return hitSomething;
}

bool WorldHitBVH(Ray ray, float tMin, float tMax, inout HitRecord rec)
{
    HitRecord tmpRec;
    float cloestSoFar = tMax;
    bool hitSomething = false;
	int curr = world.nodesHead;
	while(curr != -1 || !StackEmpty())
	{
		BVHNode currNode = GetBVHNodeFromTexture(curr);
		if(AABBHit(ray, currNode.aabb,tMin, cloestSoFar))
		{
			if(currNode.objectIndex != -1)
			{
				switch(currNode.objectType)
				{
					case OBJ_SPHERE:
						if(SphereHit(GetSphereFromTexture(currNode.objectIndex),ray, tMin, cloestSoFar,tmpRec))
						{
							rec = tmpRec;
							cloestSoFar = tmpRec.t;
							hitSomething = true;
						}
					break;
					case OBJ_XYRECT:
						if(XYRectHit(GetXYRectFromTexture(currNode.objectIndex), ray, tMin, cloestSoFar,tmpRec))
						{
							rec = tmpRec;
							cloestSoFar = tmpRec.t;
							hitSomething = true;
						}
					break;
					case OBJ_XZRECT:
						if(XZRectHit(GetXZRectFromTexture(currNode.objectIndex), ray, tMin, cloestSoFar,tmpRec))
						{
							rec = tmpRec;
							cloestSoFar = tmpRec.t;
							hitSomething = true;
						}
					break;
					case OBJ_YZRECT:
						if(YZRectHit(GetYZRectFromTexture(currNode.objectIndex), ray, tMin, cloestSoFar,tmpRec))
						{
							rec = tmpRec;
							cloestSoFar = tmpRec.t;
							hitSomething = true;
						}
					break;
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
				StackPush(currNode.right);
				curr = currNode.left;
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

vec3 WorldTrace(Ray ray, int depth)
{
    HitRecord hitRecord;

	vec3 frac = vec3(1.0, 1.0, 1.0);
	vec3 bgColor = vec3(0.0, 0.0, 0.0);
	while(depth>0)
	{
		depth--;
		// if(WorldHit(ray, 0.001, RAYCAST_MAX, hitRecord))
		if(WorldHitBVH(ray, 0.001, RAYCAST_MAX, hitRecord))
		{
			Ray scatterRay;
			vec3 attenuation;
			if(!MaterialScatter(ray, hitRecord, scatterRay, attenuation))
				break;
			
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

bool LambertianScatter(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = hitRecord.material.color;

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

bool MetallicScatter(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = hitRecord.material.color;

	scattered.origin = hitRecord.position;
	scattered.direction = reflect(incident.direction, hitRecord.normal);

	return dot(scattered.direction, hitRecord.normal) > 0.0;
}

Dielectric DielectricConstructor(vec3 albedo, float roughness, float ior)
{
	Dielectric dielectric;

	dielectric.albedo = albedo;
	dielectric.roughness = roughness;
	dielectric.ior = ior;

	return dielectric;
}

bool DielectricScatter1(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = hitRecord.material.color;
	vec3 reflected = reflect(incident.direction, hitRecord.normal);

	vec3 outward_normal;
	float ni_over_nt;
	if(dot(incident.direction, hitRecord.normal) > 0.0)// hit from inside
	{
		outward_normal = -hitRecord.normal;
		ni_over_nt = hitRecord.material.ior;
	}
	else // hit from outside
	{
		outward_normal = hitRecord.normal;
		ni_over_nt = 1.0 / hitRecord.material.ior;
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

bool DielectricScatter2(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = hitRecord.material.color;
	vec3 reflected = reflect(incident.direction, hitRecord.normal);

	vec3 outward_normal;
	float ni_over_nt;
	float cosine;
	if(dot(incident.direction, hitRecord.normal) > 0.0)// hit from inside
	{
		outward_normal = -hitRecord.normal;
		ni_over_nt = hitRecord.material.ior;
		cosine = dot(incident.direction, hitRecord.normal) / length(incident.direction); // incident angle
	}
	else // hit from outside
	{
		outward_normal = hitRecord.normal;
		ni_over_nt = 1.0 / hitRecord.material.ior;
		cosine = -dot(incident.direction, hitRecord.normal) / length(incident.direction); // incident angle
	}

	float reflect_prob;
	vec3 refracted;
	if(refract(incident.direction, outward_normal, ni_over_nt, refracted))
	{
		reflect_prob = schlick(cosine, hitRecord.material.ior);
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

bool DielectricScatter(in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	//return DielectricScatter1(dielectric, incident, hitRecord, scattered, attenuation);
	return DielectricScatter2(incident, hitRecord, scattered, attenuation);
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

bool MaterialScatter(in Ray incident, in HitRecord hitRecord, out Ray scatter, out vec3 attenuation)
{
    if(hitRecord.material.materialType==MAT_LAMBERTIAN)
		return LambertianScatter(incident, hitRecord, scatter, attenuation);
	else if(hitRecord.material.materialType==MAT_METALLIC)
		return MetallicScatter(incident, hitRecord, scatter, attenuation);
	else if(hitRecord.material.materialType==MAT_DIELECTRIC)
		return DielectricScatter(incident, hitRecord, scatter, attenuation);
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

// main function
// -------------
void main()
{
	camera = CameraConstructor(cameraParameter.lookFrom, cameraParameter.lookAt, cameraParameter.vup, 20.0, cameraParameter.aspectRatio);
	vec3 col = vec3(0.0, 0.0, 0.0);
	int ns = 100;
	for(int i=0; i<ns; i++)
	{
		Ray ray = CameraGetRay(camera, screenCoord + RandInSquare() / screenSize);
		col += WorldTrace(ray, 50);
	}
	col /= ns;

	FragColor.xyz = col;
	FragColor.w = 1.0;
}