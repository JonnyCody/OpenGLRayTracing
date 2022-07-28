#version 330 core

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
struct Ray {
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
}; 

struct HitRecord
{
	float t;
	vec3 position;
	vec3 normal;
};

struct World
{
    int objectCount;
    Sphere objects[10];
};

// global variables
// ----------------
uniform World world;
uniform float rdSeed[4];
int rdCnt = 0;

// functions declaration
// ---------------------
float RandXY(float x, float y);
float Rand();
vec2 RandInSquare();
Ray RayConstructor(vec3 origin, vec3 direction);
vec3 RayGetPointAt(Ray ray, float t);
float RayHitSphere(Ray ray, Sphere sphere);
Camera CameraConstructor(vec3 lower_left_corner, vec3 horizontal, vec3 vertical, vec3 origin);
Sphere SphereConstructor(vec3 center, float radius);
bool SphereHit(Sphere sphere, Ray ray, float t_min, float t_max, inout HitRecord hitRec);
bool WorldHit(Ray ray, float t_min, float t_max, inout HitRecord rec);
vec3 WorldTrace(Ray ray);
Ray CameraGetRay(Camera camera, vec2 offset);

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

Sphere SphereConstructor(vec3 center, float radius)
{
	Sphere sphere;

	sphere.center = center;
	sphere.radius = radius;

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

            return true;
        }

        temp = (-b + sqrt(discriminant)) / (2.0 * a);
		if(temp < t_max && temp> t_min)
		{
			hitRec.t = temp;
			hitRec.position = RayGetPointAt(ray, hitRec.t);
			hitRec.normal = (hitRec.position - sphere.center) / sphere.radius;
			
			return true;
		}

    }
    return false;
}

bool WorldHit(Ray ray, float t_min, float t_max, inout HitRecord rec)
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

vec3 WorldTrace(Ray ray)
{
    vec3 sphereCenter = vec3(0.0, 0.0, -1.0);
	Sphere sphere = SphereConstructor(sphereCenter, 0.5);
    HitRecord rec;
	if(WorldHit(ray,0.0, 1.0, rec))
	{
		return 0.5 * vec3(rec.normal.x + 1, rec.normal.y + 1, rec.normal.z + 1);
	}
	else
	{
		vec3 unit_direction = normalize(ray.direction);
		float t = 0.5 * (unit_direction.y + 1.0);
		return (1.0 - t) * vec3(1.0, 1.0, 1.0) + t * vec3(0.5, 0.7, 1.0);
	}
}

Ray CameraGetRay(Camera camera, vec2 offset)
{
    Ray ray = RayConstructor(camera.origin, camera.lower_left_corner + 
    offset.x*camera.horizontal + offset.y * camera.vertical);
    return ray;
}

// main function
// -------------
void main()
{
	Camera camera = CameraConstructor(vec3(-2.0, -1.5, -1.5), vec3(4.0, 0.0, 0.0), vec3(0.0, 3.0, 0.0), vec3(0.0, 0.0, 0.0));
	Ray ray = RayConstructor(camera.origin, camera.lower_left_corner + screenCoord.x * camera.horizontal + screenCoord.y * camera.vertical);
    vec3 col = vec3(0.0, 0.0, 0.0);
	int ns = 100;
	for(int i=0; i<ns; i++)
	{
		Ray ray = CameraGetRay(camera, screenCoord + RandInSquare() / screenSize);
		col += WorldTrace(ray);
	}
	col /= ns;
	FragColor.xyz = col;
	FragColor.w = 1.0;
}