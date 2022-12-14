#include <glad/glad.h>
#include <GLFW/glfw3.h>

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include <learnopengl/shader_m.h>
#include <learnopengl/camera.h>
#include <learnopengl/filesystem.h>
#include <learnopengl/model.h>
#include <raytracing/sphere.h>
#include <raytracing/bvh.h>
#include <raytracing/scene.h>
#include <raytracing/hittable_list.h>
#include <iostream>
#include <vector>
#include <map>
#include <iostream>
#include <random>

// #define STB_IMAGE_IMPLEMENTATION
// #include "stb_image.h"

void framebuffer_size_callback(GLFWwindow* window, int width, int height);
void mouse_callback(GLFWwindow* window, double xposIn, double yposIn);
void scroll_callback(GLFWwindow* window, double xoffset, double yoffset);
void processInput(GLFWwindow* window);
unsigned int loadTexture(const char* path);
unsigned int loadCubemap(std::vector<std::string> faces);
void SortObjects(HittableList& objects);
void WriteObjectsData();
void WriteBVHNodesData();
void WriteTrianglesData(Model& m);
AABB AABBofModel(Model& m);

// settings
const unsigned int SCR_WIDTH = 1080;
const unsigned int SCR_HEIGHT = 720;
const unsigned int BIG_DATA_SIZE = 100000;

const int MAT_LAMBERTIAN = 0;
const int MAT_METALLIC =  1;
const int MAT_DIELECTRIC = 2;
const int MAT_PBR =  3;
const int OBJ_SPHERE = 1;
const int OBJ_XYRECT = 2;
const int OBJ_XZRECT = 3;
const int OBJ_YZRECT = 4;
const int OBJ_MODEL = 5;    

//Camera camera(glm::vec3(-5.0f, 4.0f, 4.0f));
Camera camera(glm::vec3(13.0f, 2.0f, 3.0f));
// Camera camera(glm::vec3(278.0f, 278.0f, -800.0f));
float lastX = SCR_WIDTH / 2.0f;
float lastY = SCR_HEIGHT / 2.0f;
bool firstMouse = true;

// timing
float deltaTime = 0.0f;
float lastFrame = 0.0f;

// spheres
HittableList objects;
std::vector<BVHNode> BVHNodes;
float (*objectsData)[4] = new float[BIG_DATA_SIZE][4];
float (*BVHNodesData)[4] = new float[BIG_DATA_SIZE][4];
float (*triangleData)[4] = new float[BIG_DATA_SIZE][4];

// void 
int main()
{
    // glfw: initialize and configure
    // ------------------------------
    glfwInit();
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
    
    // window create
    // -------------
    GLFWwindow* window = glfwCreateWindow(SCR_WIDTH, SCR_HEIGHT, "OpenGLRayTracing", NULL, NULL);
    if (window == NULL)
    {
        std::cout << "Failed to create GLFW window" << std::endl;
        glfwTerminate();
        return -1;
    }

    glfwMakeContextCurrent(window);
    glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);
    glfwSetCursorPosCallback(window, mouse_callback);
    glfwSetScrollCallback(window, scroll_callback);

    glfwSetInputMode(window, GLFW_CURSOR, GLFW_CURSOR_DISABLED);

    // glad: load all OpenGL function pointers
    // ---------------------------------------
    if (!gladLoadGLLoader((GLADloadproc)glfwGetProcAddress))
    {
        std::cout << "Failed to initialize GLAD" << std::endl;
        return -1;
    }

    // build and compile shaders
    Shader shader(FileSystem::getPath("src/ray_tracing_optimize/ray_tracing_optimize.vs").c_str(),
         FileSystem::getPath("src/ray_tracing_optimize/ray_tracing_optimize.fs").c_str());

    Model model(FileSystem::getPath("resources/objects/rock/rock.obj"));
    // Model model(FileSystem::getPath("resources/objects/bunny/bunny.obj"));
    float vertices[] = 
    {
			 1.0f,  1.0f, 0.0f,  // top right
			 1.0f, -1.0f, 0.0f,  // bottom right
			-1.0f, -1.0f, 0.0f,  // bottom left
			-1.0f,  1.0f, 0.0f   // top left 
    };

    unsigned int indices[] = {  // note that we start from 0!
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    };

    unsigned int VBO, VAO, EBO;
    glGenVertexArrays(1, &VAO);
    glGenBuffers(1, &VBO);
    glGenBuffers(1, &EBO);
    // bind the Vertex Array Object first, then bind and set vertex buffer(s), and then configure vertex attributes(s).
    glBindVertexArray(VAO);

    glBindBuffer(GL_ARRAY_BUFFER, VBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);

    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices), indices, GL_DYNAMIC_DRAW);
    
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);
    
    float skyboxVertices[] = {
        // positions          
        -1.0f,  1.0f, -1.0f,
        -1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

        -1.0f,  1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f,  1.0f
    };

    std::vector<std::string> faces
    {
        FileSystem::getPath("resources/textures/skybox/right.jpg"),
        FileSystem::getPath("resources/textures/skybox/left.jpg"),
        FileSystem::getPath("resources/textures/skybox/top.jpg"),
        FileSystem::getPath("resources/textures/skybox/bottom.jpg"),
        FileSystem::getPath("resources/textures/skybox/front.jpg"),
        FileSystem::getPath("resources/textures/skybox/back.jpg")
    };

    // skybox VAO
    // ----------
    // skybox VAO
    unsigned int skyboxVAO, skyboxVBO;
    glGenVertexArrays(1, &skyboxVAO);
    glGenBuffers(1, &skyboxVBO);
    glBindVertexArray(skyboxVAO);
    glBindBuffer(GL_ARRAY_BUFFER, skyboxVBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(skyboxVertices), &skyboxVertices, GL_STATIC_DRAW);
    glEnableVertexAttribArray(0);
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)0);

    unsigned int cubemapTexture = loadCubemap(faces);

    // create tbo data
    // ---------------
    AABB aabbModel = AABBofModel(model);
    // Scene1(objects, aabbModel);
    DisplayScene(objects, aabbModel);
    // RandomScene(objects);
    // CornellBox(objects);
    SortObjects(objects);
    WriteObjectsData();
    WriteBVHNodesData();
    WriteTrianglesData(model);
    cout << aabbModel.minimum[0] << " " << aabbModel.minimum[1] << " " << aabbModel.minimum[2] << endl;
    cout << aabbModel.maximum[0] << " " << aabbModel.maximum[1] << " " << aabbModel.maximum[2] << endl;
    
    // generate buffer texture
    // -----------------------
    unsigned int tboSpheresId[3], tboBufferId[3];
    glGenTextures(3, tboSpheresId);
    glGenBuffers(3, tboBufferId);
    glBindBuffer(GL_TEXTURE_BUFFER, tboBufferId[0]);
    glBufferData(GL_TEXTURE_BUFFER, sizeof(float) * BIG_DATA_SIZE * 4, objectsData, GL_STATIC_DRAW);
    glBindBuffer(GL_TEXTURE_BUFFER, tboBufferId[1]);
    glBufferData(GL_TEXTURE_BUFFER, sizeof(float) * BIG_DATA_SIZE * 4, BVHNodesData, GL_STATIC_DRAW);
    glBindBuffer(GL_TEXTURE_BUFFER, tboBufferId[2]);
    glBufferData(GL_TEXTURE_BUFFER, sizeof(float) * BIG_DATA_SIZE * 4, triangleData, GL_STATIC_DRAW);

    shader.use();
    shader.setInt("objectsData", 0);
    shader.setInt("BVHNodesData", 1);
    shader.setInt("trianglesData", 2);
    shader.setInt("envMap", 3);

    // render loop
    // -----------
    while (!glfwWindowShouldClose(window))
    {
        // per-frame time logic
        // --------------------
        float currentFrame = static_cast<float>(glfwGetTime());
        deltaTime = currentFrame - lastFrame;
        lastFrame = currentFrame;

        // input
        // -----
        processInput(window);

        // render
        // ------
        glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        /*glBindBuffer(GL_UNIFORM_BUFFER, uboSpheres);
        glBufferData(GL_UNIFORM_BUFFER, sizeof(spheresParamter), &spheresParamter, GL_STATIC_DRAW);
        glBindBufferRange(GL_UNIFORM_BUFFER, 0, uboSpheres, 0, sizeof(spheresParamter));
        glBindBuffer(GL_UNIFORM_BUFFER, 0);*/
        
        shader.use();

        shader.setVec2("screenSize", { SCR_WIDTH, SCR_HEIGHT });
        shader.setVec3("cameraParameter.lookFrom", camera.Position);
        shader.setVec3("cameraParameter.lookAt", camera.Position + camera.Front);
        shader.setVec3("cameraParameter.vup", camera.WorldUp);
        shader.setFloat("cameraParameter.vfov", 20.0);
        shader.setFloat("cameraParameter.aspectRatio", (float)SCR_WIDTH/SCR_HEIGHT);
        shader.setInt("world.objectCount", objects.size());
        shader.setInt("world.nodesHead", BVHNodes.size() - 1);
        shader.setInt("world.triangleCount", model.meshes[0].indices.size() / 3);
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_BUFFER, tboSpheresId[0]);
        glTexBuffer(GL_TEXTURE_BUFFER, GL_RGBA32F, tboBufferId[0]);
        glActiveTexture(GL_TEXTURE1);
        glBindTexture(GL_TEXTURE_BUFFER, tboSpheresId[1]);
        glTexBuffer(GL_TEXTURE_BUFFER, GL_RGBA32F, tboBufferId[1]);
        glActiveTexture(GL_TEXTURE2);
        glBindTexture(GL_TEXTURE_BUFFER, tboSpheresId[2]);
        glTexBuffer(GL_TEXTURE_BUFFER, GL_RGBA32F, tboBufferId[2]);
    
        glBindVertexArray(VAO);
        
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0);
        glBindVertexArray(skyboxVAO);
        glActiveTexture(GL_TEXTURE3);
        glBindTexture(GL_TEXTURE_CUBE_MAP, cubemapTexture);
        glDrawArrays(GL_TRIANGLES, 0, 36);
        glBindVertexArray(0);
        glDepthFunc(GL_LESS);
        
        /*std::cout << camera.Position[0] << " " << camera.Position[1] << " " << camera.Position[2] << std::endl;
        std::cout << camera.Front[0] << " " << camera.Front[1] << " " << camera.Front[2] << std::endl;*/

        // glfw: swap buffers and poll IO events (keys pressed/released, mouse moved etc.)
        // -------------------------------------------------------------------------------
        glfwSwapBuffers(window);
        glfwPollEvents();
    }

    // optional: de-allocate all resources once they've outlived their purpose:
    // ------------------------------------------------------------------------
    glDeleteVertexArrays(1, &VAO);
    glDeleteBuffers(1, &VBO);
    glDeleteBuffers(1, &EBO);
    glDeleteBuffers(3, tboBufferId);
    delete[] objectsData;
    delete[] BVHNodesData;
    delete[] triangleData;

    // glfw: terminate, clearing all previously allocated GLFW resources.
    // ------------------------------------------------------------------
    glfwTerminate();
    return 0;
}

void framebuffer_size_callback(GLFWwindow* window, int width, int height)
{
	glViewport(0, 0, width, height);
}

void mouse_callback(GLFWwindow* window, double xposIn, double yposIn)
{
	float xpos = static_cast<float> (xposIn);
	float ypos = static_cast<float> (yposIn);

	if (firstMouse)
	{
		lastX = xpos;
		lastY = ypos;
		firstMouse = false;
	}

	float xoffset = xpos - lastX;
	float yoffset = lastY - ypos;

	lastX = xpos;
	lastY = ypos;

	camera.ProcessMouseMovement(xoffset, yoffset);
}

void scroll_callback(GLFWwindow* window, double xoffset, double yoffset)
{
	camera.ProcessMouseScroll(static_cast<float>(yoffset));
}

void processInput(GLFWwindow* window)
{
	if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
		glfwSetWindowShouldClose(window, true);

	if (glfwGetKey(window, GLFW_KEY_W) == GLFW_PRESS)
		camera.ProcessKeyboard(FORWARD, deltaTime);
	if (glfwGetKey(window, GLFW_KEY_S) == GLFW_PRESS)
		camera.ProcessKeyboard(BACKWARD, deltaTime);
	if (glfwGetKey(window, GLFW_KEY_A) == GLFW_PRESS)
		camera.ProcessKeyboard(LEFT, deltaTime);
	if (glfwGetKey(window, GLFW_KEY_D) == GLFW_PRESS)
		camera.ProcessKeyboard(RIGHT, deltaTime);
}
unsigned int loadTexture(const char* path)
{
    // texture
    unsigned int textureID;
    glGenTextures(1, &textureID);
    glBindTexture(GL_TEXTURE_2D, textureID);

    // load image, create texture and generate mipmaps
    int width, height, nrComponents;
    unsigned char* data = stbi_load(path, &width, &height, &nrComponents, 0);
    if (data)
    {
        GLenum format;
        if (nrComponents == 1)
            format = GL_RED;
        else if (nrComponents == 3)
            format = GL_RGB;
        else if (nrComponents == 4)
            format = GL_RGBA;

        glBindTexture(GL_TEXTURE_2D, textureID);
        glTexImage2D(GL_TEXTURE_2D, 0, format, width, height, 0, format, GL_UNSIGNED_BYTE, data);
        glGenerateMipmap(GL_TEXTURE_2D);

        // set the texture wrapping/filtering option 
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        // set texture filtering parameters
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

        stbi_image_free(data);
    }
    else
    {
        std::cout << "Failed to load texture" << std::endl;
        stbi_image_free(data);
    }
    return textureID;
}
unsigned int loadCubemap(std::vector<std::string> faces)
{
    unsigned int textureID;
    glGenTextures(1, &textureID);
    glBindTexture(GL_TEXTURE_CUBE_MAP, textureID);

    int width, height, nrChannels;
    for (unsigned int i = 0; i < faces.size(); ++i)
    {
        unsigned char* data = stbi_load(faces[i].c_str(), &width, &height, &nrChannels, 0);
        if (data)
        {
            glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i,
                0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, data);
            stbi_image_free(data);
        }
        else
        {
            std::cout<< "Cubemap texture failed to load at path: " << faces[i] << std::endl;
            stbi_image_free(data);
        }
    }
    glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE);

    return textureID;
}

void SortObjects(HittableList& objects)
{
    static std::default_random_engine e;
    static std::uniform_int_distribution<unsigned> u(0, 2);

    unsigned span = objects.size();
    unsigned start = 0;
    unsigned axis = 0;
    while(span >= 2)
    {
        while (start < objects.size() - 1)
        {
            axis = u(e);
            auto end = objects.begin() + span > objects.end() ? objects.end() : objects.begin() + span;
            std::sort(objects.begin() + start, objects.begin() + span, [axis](std::shared_ptr<Hittable> a,std::shared_ptr<Hittable>b){
                return a->box.min()[axis] < b->box.min()[axis];
            });
            start += span;
        }
        span /= 2;
    }
}

void WriteObjectsData()
{
    for(int i = 0; i < objects.size(); ++i)
    {
        switch(objects[i]->objectType)
        {
            case OBJ_SPHERE:
                objectsData[i*3][0] = objects[i]->center[0];
                objectsData[i*3][1] = objects[i]->center[1];
                objectsData[i*3][2] = objects[i]->center[2];
                objectsData[i*3][3] = objects[i]->radius;
                objectsData[i*3+1][0] = objects[i]->matPtr->color[0];
                objectsData[i*3+1][1] = objects[i]->matPtr->color[1];
                objectsData[i*3+1][2] = objects[i]->matPtr->color[2];
                objectsData[i*3+1][3] = objects[i]->matPtr->materialType;
                objectsData[i*3+2][0] = objects[i]->matPtr->roughness;
                objectsData[i*3+2][1] = objects[i]->matPtr->ior;
            break;
            case OBJ_XYRECT:
                objectsData[i*3][0] = objects[i]->x0;
                objectsData[i*3][1] = objects[i]->x1;
                objectsData[i*3][2] = objects[i]->y0;
                objectsData[i*3][3] = objects[i]->y1;
                objectsData[i*3+1][0] = objects[i]->matPtr->color[0];
                objectsData[i*3+1][1] = objects[i]->matPtr->color[1];
                objectsData[i*3+1][2] = objects[i]->matPtr->color[2];
                objectsData[i*3+1][3] = objects[i]->k;
                objectsData[i*3+2][0] = objects[i]->matPtr->materialType;
                objectsData[i*3+2][1] = objects[i]->matPtr->roughness;
                objectsData[i*3+2][2] = objects[i]->matPtr->ior;
                
            break;
            case OBJ_XZRECT:
                objectsData[i*3][0] = objects[i]->x0;
                objectsData[i*3][1] = objects[i]->x1;
                objectsData[i*3][2] = objects[i]->z0;
                objectsData[i*3][3] = objects[i]->z1;
                objectsData[i*3+1][0] = objects[i]->matPtr->color[0];
                objectsData[i*3+1][1] = objects[i]->matPtr->color[1];
                objectsData[i*3+1][2] = objects[i]->matPtr->color[2];
                objectsData[i*3+1][3] = objects[i]->k;
                objectsData[i*3+2][0] = objects[i]->matPtr->materialType;
                objectsData[i*3+2][1] = objects[i]->matPtr->roughness;
                objectsData[i*3+2][2] = objects[i]->matPtr->ior;
            break;
            case OBJ_YZRECT:
                objectsData[i*3][0] = objects[i]->y0;
                objectsData[i*3][1] = objects[i]->y1;
                objectsData[i*3][2] = objects[i]->z0;
                objectsData[i*3][3] = objects[i]->z1;
                objectsData[i*3+1][0] = objects[i]->matPtr->color[0];
                objectsData[i*3+1][1] = objects[i]->matPtr->color[1];
                objectsData[i*3+1][2] = objects[i]->matPtr->color[2];
                objectsData[i*3+1][3] = objects[i]->k;
                objectsData[i*3+2][0] = objects[i]->matPtr->materialType;
                objectsData[i*3+2][1] = objects[i]->matPtr->roughness;
                objectsData[i*3+2][2] = objects[i]->matPtr->ior;
            break;
        }
        
    }
}

void WriteBVHNodesData()
{
    BVHNodes.resize(objects.size() * 2 - 1);
    BuildBVHNodes(BVHNodes, objects);
    for (int i = 0; i < BVHNodes.size(); ++i)
    {
        BVHNodesData[3 * i][0] = BVHNodes[i].aabb.minimum[0];
        BVHNodesData[3 * i][1] = BVHNodes[i].aabb.minimum[1];
        BVHNodesData[3 * i][2] = BVHNodes[i].aabb.minimum[2];
        BVHNodesData[3 * i][3] = BVHNodes[i].objectIndex;
        BVHNodesData[3 * i + 1][0] = BVHNodes[i].aabb.maximum[0];
        BVHNodesData[3 * i + 1][1] = BVHNodes[i].aabb.maximum[1];
        BVHNodesData[3 * i + 1][2] = BVHNodes[i].aabb.maximum[2];
        BVHNodesData[3 * i + 1][3] = BVHNodes[i].objectType;
        BVHNodesData[3 * i + 2][0] = BVHNodes[i].left;
        BVHNodesData[3 * i + 2][1] = BVHNodes[i].right;
    }
    // for(auto & i:BVHNodes)
    // {
    //     std::cout << i.left << " " << i.right << " " << i.parent << " " << i.objectIndex << std::endl;
    // }
}

void WriteTrianglesData(Model& m)
{
    int triangleIndex = 0;
    for(int i = 0; i < m.meshes.size(); ++i)
    {
        for( int j = 0; j < m.meshes[i].vertices.size(); ++j)
        {
            triangleData[triangleIndex][0] = m.meshes[i].vertices[j].Position[0];
            triangleData[triangleIndex][1] = m.meshes[i].vertices[j].Position[1];
            triangleData[triangleIndex][2] = m.meshes[i].vertices[j].Position[2];
            triangleData[triangleIndex][3] = m.meshes[i].vertices[j].TexCoords[0];
            triangleData[triangleIndex + 1][0] = m.meshes[i].vertices[j].Normal[0];
            triangleData[triangleIndex + 1][1] = m.meshes[i].vertices[j].Normal[1];
            triangleData[triangleIndex + 1][2] = m.meshes[i].vertices[j].Normal[2];
            triangleData[triangleIndex + 1][3] = m.meshes[i].vertices[j].TexCoords[1];
            triangleIndex+=2;
        }
    }
}

AABB AABBofModel(Model& m)
{
    bool first = true;
    AABB merge, tri;
    for(int i = 0; i < m.meshes.size(); ++i)
    {
        for(int j = 0; j < m.meshes[i].vertices.size(); j+=3)
        {
            if(first)
            {
                merge = AABB(vec3(fmin(m.meshes[i].vertices[j].Position[0], 
                                fmin(m.meshes[i].vertices[j+1].Position[0], m.meshes[i].vertices[j+2].Position[0])),
                                fmin(m.meshes[i].vertices[j].Position[1], 
                                fmin(m.meshes[i].vertices[j+1].Position[1], m.meshes[i].vertices[j+2].Position[1])),
                                fmin(m.meshes[i].vertices[j].Position[2], 
                                fmin(m.meshes[i].vertices[j+1].Position[2], m.meshes[i].vertices[j+2].Position[2]))), 
                            vec3(fmax(m.meshes[i].vertices[j].Position[0], 
                                fmax(m.meshes[i].vertices[j+1].Position[0], m.meshes[i].vertices[j+2].Position[0])),
                                fmax(m.meshes[i].vertices[j].Position[1], 
                                fmax(m.meshes[i].vertices[j+1].Position[1], m.meshes[i].vertices[j+2].Position[1])),
                                fmax(m.meshes[i].vertices[j].Position[2], 
                                fmax(m.meshes[i].vertices[j+1].Position[2], m.meshes[i].vertices[j+2].Position[2]))));
                first = false;
            }
            else
            {
                tri = AABB(vec3(fmin(m.meshes[i].vertices[j].Position[0], 
                                fmin(m.meshes[i].vertices[j+1].Position[0], m.meshes[i].vertices[j+2].Position[0])),
                                fmin(m.meshes[i].vertices[j].Position[1], 
                                fmin(m.meshes[i].vertices[j+1].Position[1], m.meshes[i].vertices[j+2].Position[1])),
                                fmin(m.meshes[i].vertices[j].Position[2], 
                                fmin(m.meshes[i].vertices[j+1].Position[2], m.meshes[i].vertices[j+2].Position[2]))), 
                            vec3(fmax(m.meshes[i].vertices[j].Position[0], 
                                fmax(m.meshes[i].vertices[j+1].Position[0], m.meshes[i].vertices[j+2].Position[0])),
                                fmax(m.meshes[i].vertices[j].Position[1], 
                                fmax(m.meshes[i].vertices[j+1].Position[1], m.meshes[i].vertices[j+2].Position[1])),
                                fmax(m.meshes[i].vertices[j].Position[2], 
                                fmax(m.meshes[i].vertices[j+1].Position[2], m.meshes[i].vertices[j+2].Position[2]))));
                merge = SurroundingBox(merge, tri);
            }
        }
    }
    return merge;
}