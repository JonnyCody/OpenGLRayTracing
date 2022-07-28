#version 330 core
#define PI 3.14159265
#define RAYCAST_MAX 100000.0
#define MAT_LAMBERTIAN      0
#define MAT_METALLIC        1
#define MAT_DIELECTRIC      2
#define MAT_PBR             3
// in variables
// ------------
in vec2 screenCoord;

// textures
// --------
uniform sampler2D diffuseMap;
uniform sampler2D specularMap;
uniform sampler2D envMap;
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

struct Camera 
{
    vec3 lower_left_corner;
    vec3 horizontal;
	vec3 vertical;
	vec3 origin;
}; 

struct Sphere 
{
    vec3 center;
    float radius;
    int materialType;
    int material;
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
    Sphere objects[10];
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
uniform float rdSeed[4];
int rdCnt = 0;

World world;
Camera camera;
Lambertian lambertMaterials[4];
Metallic metallicMaterials[4];
Dielectric dielectricMaterials[4];

// functions declaration
// ---------------------
float RandXY(float x, float y);
float Rand();
vec2 RandInSquare();
vec3 RandInSphere();
Ray RayConstructor(vec3 origin, vec3 direction);
vec3 RayGetPointAt(Ray ray, float t);
float RayHitSphere(Ray ray, Sphere sphere);
Camera CameraConstructor(vec3 lower_left_corner, vec3 horizontal, vec3 vertical, vec3 origin);
Sphere SphereConstructor(vec3 center, float radius);
bool SphereHit(Sphere sphere, Ray ray, float t_min, float t_max, inout HitRecord hitRec);
World WorldConstructor();
bool WorldHit(World world, Ray ray, float t_min, float t_max, inout HitRecord rec);
vec3 WorldTrace(World world, Ray ray, int depth);
Ray CameraGetRay(Camera camera, vec2 offset);
vec3 GetEnvironmentColor(World world, Ray ray);
Lambertian LambertianConstructor(vec3 albedo);
Metallic MetallicConstructor(vec3 albedo, float roughness);
bool MetallicScatter(in Metallic metallic, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool LambertianScatter(in Lambertian lambertian, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool MaterialScatter(in int materialType, in int material, in Ray incident, in HitRecord hitRecord, out Ray scatter, out vec3 attenuation);
Dielectric DielectricConstructor(vec3 albedo, float roughness, float ior);
bool DielectricScatter1(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool DielectricScatter2(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
bool DielectricScatter(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation);
float schlick(float cosine, float ior);
vec3 reflect(in vec3 incident, in vec3 normal);
bool refract(vec3 v, vec3 n, float niOverNt, out vec3 refracted);

void InitScene();

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

Camera CameraConstructor(vec3 lower_left_corner, vec3 horizontal, vec3 vertical, vec3 origin)
{
	Camera camera;

	camera.lower_left_corner = lower_left_corner;
	camera.horizontal = horizontal;
	camera.vertical = vertical;
	camera.origin = origin;

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

World WorldConstructor()
{
	World world;

	world.objectCount = 4;
	world.objects[0] = SphereConstructor(vec3( 0.0,    0.0, -1.0), 0.5, MAT_LAMBERTIAN, 0);
	world.objects[1] = SphereConstructor(vec3( 0.7,    0.0, -1.0), 0.5, MAT_METALLIC, 1);
	world.objects[2] = SphereConstructor(vec3(-0.7,    0.0, -1.0), 0.5, MAT_DIELECTRIC, 2);
	world.objects[3] = SphereConstructor(vec3( 0.0, -100.5, -1.0), 100.0, MAT_LAMBERTIAN, 3);

	return world;
}

bool WorldHit(World world, Ray ray, float t_min, float t_max, inout HitRecord rec)
{
    HitRecord tmpRec;
    float cloestSoFar = t_max;
    bool hitSomething = false;

    for(int i = 0; i < world.objectCount; ++i)
    {
        if(SphereHit(world.objects[i], ray, t_min, cloestSoFar, tmpRec))
        {
            rec = tmpRec;
            cloestSoFar = tmpRec.t;

            hitSomething = true;
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
		{
			Ray scatterRay;
			vec3 attenuation;
			if(!MaterialScatter(hitRecord.materialType, hitRecord.material, ray, hitRecord, scatterRay, attenuation))
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

Ray CameraGetRay(Camera camera, vec2 offset)
{
    Ray ray = RayConstructor(camera.origin, camera.lower_left_corner + 
    offset.x*camera.horizontal + offset.y * camera.vertical);
    return ray;
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

void InitScene()
{
	world = WorldConstructor();
	camera = CameraConstructor(vec3(-2.0, -1.5, -1.0), vec3(4.0, 0.0, 0.0), vec3(0.0, 3.0, 0.0), vec3(0.0, 0.0, 0.0));

	lambertMaterials[0] = LambertianConstructor(vec3(0.1, 0.2, 0.5));
	lambertMaterials[1] = LambertianConstructor(vec3(0.5, 0.7, 0.5));
	lambertMaterials[2] = LambertianConstructor(vec3(0.5, 0.5, 0.7));
	lambertMaterials[3] = LambertianConstructor(vec3(0.8, 0.8, 0.0));

	metallicMaterials[0] = MetallicConstructor(vec3(0.7, 0.5, 0.5), 0.0);
	metallicMaterials[1] = MetallicConstructor(vec3(0.5, 0.7, 0.5), 0.1);
	metallicMaterials[2] = MetallicConstructor(vec3(0.5, 0.5, 0.7), 0.2);
	metallicMaterials[3] = MetallicConstructor(vec3(0.7, 0.7, 0.7), 0.3);

	dielectricMaterials[0] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.0, 1.5);
	dielectricMaterials[1] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.1, 1.5);
	dielectricMaterials[2] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.2, 1.5);
	dielectricMaterials[3] = DielectricConstructor(vec3(1.0, 1.0, 1.0), 0.3, 1.5);
}

// main function
// -------------
void main()
{
	InitScene();
	
	vec3 col = vec3(0.0, 0.0, 0.0);
	int ns = 100;
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