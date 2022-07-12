#include <glad/glad.h>
#include <GLFW/glfw3.h>

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include <learnopengl/shader_m.h>
#include <learnopengl/camera.h>
#include <learnopengl/filesystem.h>
#include <iostream>
#include <vector>
#include <map>

#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

void framebuffer_size_callback(GLFWwindow* window, int width, int height);
void mouse_callback(GLFWwindow* window, double xposIn, double yposIn);
void scroll_callback(GLFWwindow* window, double xoffset, double yoffset);
void processInput(GLFWwindow* window);
unsigned int loadTexture(const char* path);
unsigned int loadCubemap(std::vector<std::string> faces);

// settings
const unsigned int SCR_WIDTH = 800;
const unsigned int SCR_HEIGHT = 600;
const GLfloat PI = 3.14159265358979323846f;
const int Y_SEGMENTS = 50;
const int X_SEGMENTS = 50;

Camera camera(glm::vec3(0.0f, 0.0f, 3.0f));
float lastX = SCR_WIDTH / 2.0f;
float lastY = SCR_HEIGHT / 2.0f;
bool firstMouse = true;

// timing
float deltaTime = 0.0f;
float lastFrame = 0.0f;

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

    // configure global opengl state
    // -----------------------------
    glEnable(GL_DEPTH_TEST);
    glDepthFunc(GL_LESS);

    // build and compile shaders
    Shader skyboxShader(FileSystem::getPath("src/skybox/skybox.vs").c_str(),
        FileSystem::getPath("src/skybox/skybox.fs").c_str());
    Shader shader(FileSystem::getPath("src/skybox/balls_earth.vs").c_str(),
         FileSystem::getPath("src/skybox/balls_earth.fs").c_str());

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

    // load textures
    // -------------
    unsigned int cubemapTexture = loadCubemap(faces);
    unsigned int sphereTexture = loadTexture(FileSystem::getPath("resources/textures/earthmap.jpg").c_str());

    // shader configuration
    // --------------------
    skyboxShader.use();
    skyboxShader.setInt("skybox", 0);

    shader.use();
    shader.setInt("texture1", 0);

    // produce sphere position
    // -----------------------
    std::vector<float> spherePoints;
    std::vector<float> sphereTexCoords;
    std::vector<float> sphereNormals;
    std::vector<float> sphereVertices;
    std::vector<int> sphereIndices;
    for(int y = 0; y <= Y_SEGMENTS; ++y)
    {
        for(int x = 0; x <= X_SEGMENTS; ++x)
        {
            float xSegment = (float)x / (float)X_SEGMENTS;
            float ySegment = (float)y / (float)Y_SEGMENTS;
            float xPos = std::cos(xSegment * 2.0f * PI) * std::sin(ySegment * PI);
            float yPos = std::cos(ySegment * PI);
            float zPos = std::sin(xSegment * 2.0f * PI) * std::sin(ySegment * PI);
            spherePoints.push_back(xPos);
            spherePoints.push_back(yPos);
            spherePoints.push_back(zPos);
            sphereTexCoords.push_back(xSegment);
            sphereTexCoords.push_back(ySegment);
        }
    }

    for (int i=0;i<Y_SEGMENTS;i++)
	{
		for (int j=0;j<X_SEGMENTS;j++)
		{
			sphereIndices.push_back(i * (X_SEGMENTS + 1) + j);
            sphereVertices.push_back(spherePoints[3*(i * (X_SEGMENTS + 1) + j)]);
            sphereVertices.push_back(spherePoints[3*(i * (X_SEGMENTS + 1) + j) + 1]);
            sphereVertices.push_back(spherePoints[3*(i * (X_SEGMENTS + 1) + j) + 2]);
            sphereVertices.push_back(sphereTexCoords[2*(i * (X_SEGMENTS + 1) + j)]);
            sphereVertices.push_back(sphereTexCoords[2*(i * (X_SEGMENTS + 1) + j) + 1]);

			sphereIndices.push_back((i + 1) * (X_SEGMENTS + 1) + j);
            sphereVertices.push_back(spherePoints[3*((i + 1) * (X_SEGMENTS + 1) + j)]);
            sphereVertices.push_back(spherePoints[3*((i + 1) * (X_SEGMENTS + 1) + j) + 1]);
            sphereVertices.push_back(spherePoints[3*((i + 1) * (X_SEGMENTS + 1) + j) + 2]);
            sphereVertices.push_back(sphereTexCoords[2*((i + 1) * (X_SEGMENTS + 1) + j)]);
            sphereVertices.push_back(sphereTexCoords[2*((i + 1) * (X_SEGMENTS + 1) + j) + 1]);

			sphereIndices.push_back((i + 1) * (X_SEGMENTS + 1) + j + 1);
            sphereVertices.push_back(spherePoints[3*((i + 1) * (X_SEGMENTS + 1) + j + 1)]);
            sphereVertices.push_back(spherePoints[3*((i + 1) * (X_SEGMENTS + 1) + j + 1) + 1]);
            sphereVertices.push_back(spherePoints[3*((i + 1) * (X_SEGMENTS + 1) + j + 1) + 2]);
            sphereVertices.push_back(sphereTexCoords[2*((i + 1) * (X_SEGMENTS + 1) + j + 1)]);
            sphereVertices.push_back(sphereTexCoords[2*((i + 1) * (X_SEGMENTS + 1) + j + 1) + 1]);

			sphereIndices.push_back(i * (X_SEGMENTS + 1) + j);
            sphereVertices.push_back(spherePoints[3*(i * (X_SEGMENTS + 1) + j)]);
            sphereVertices.push_back(spherePoints[3*(i * (X_SEGMENTS + 1) + j) + 1]);
            sphereVertices.push_back(spherePoints[3*(i * (X_SEGMENTS + 1) + j) + 2]);
            sphereVertices.push_back(sphereTexCoords[2*(i * (X_SEGMENTS + 1) + j)]);
            sphereVertices.push_back(sphereTexCoords[2*(i * (X_SEGMENTS + 1) + j) + 1]);

			sphereIndices.push_back((i + 1) * (X_SEGMENTS + 1) + j + 1);
            sphereVertices.push_back(spherePoints[3*((i + 1) * (X_SEGMENTS + 1) + j + 1)]);
            sphereVertices.push_back(spherePoints[3*((i + 1) * (X_SEGMENTS + 1) + j + 1) + 1]);
            sphereVertices.push_back(spherePoints[3*((i + 1) * (X_SEGMENTS + 1) + j + 1) + 2]);
            sphereVertices.push_back(sphereTexCoords[2*((i + 1) * (X_SEGMENTS + 1) + j + 1)]);
            sphereVertices.push_back(sphereTexCoords[2*((i + 1) * (X_SEGMENTS + 1) + j + 1) + 1]);

			sphereIndices.push_back(i * (X_SEGMENTS + 1) + j + 1);
            sphereVertices.push_back(spherePoints[3*(i * (X_SEGMENTS + 1) + j + 1)]);
            sphereVertices.push_back(spherePoints[3*(i * (X_SEGMENTS + 1) + j + 1) + 1]);
            sphereVertices.push_back(spherePoints[3*(i * (X_SEGMENTS + 1) + j + 1) + 2]);
            sphereVertices.push_back(sphereTexCoords[2*(i * (X_SEGMENTS + 1) + j + 1)]);
            sphereVertices.push_back(sphereTexCoords[2*(i * (X_SEGMENTS + 1) + j + 1) + 1]);
		}
	}

    unsigned sphereVAO, sphereVBO;
    glGenVertexArrays(1, &sphereVAO);
    glGenBuffers(1, &sphereVBO);
    
    glBindVertexArray(sphereVAO);
    glBindBuffer(GL_ARRAY_BUFFER, sphereVBO);
    glBufferData(GL_ARRAY_BUFFER, sphereVertices.size() * sizeof(float), &sphereVertices[0], GL_STATIC_DRAW);
    

    // unsigned int sphereEBO;
    // glGenBuffers(1, &sphereEBO);
    // glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, sphereEBO);
    // glBufferData(GL_ELEMENT_ARRAY_BUFFER, sphereIndices.size() * sizeof(int), &sphereIndices[0], GL_STATIC_DRAW);

    glEnableVertexAttribArray(0);
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(1);
    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)(3*sizeof(float)));
    glBindVertexArray(0);

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

        shader.use();
        /*glEnable(GL_CULL_FACE);
        glCullFace(GL_BACK);*/
        glm::mat4 model = glm::mat4(1.0f);
        glm::mat4 projection = glm::perspective(glm::radians(camera.Zoom), (float)SCR_WIDTH / (float)SCR_HEIGHT, 0.1f, 100.0f);
        glm::mat4 view = camera.GetViewMatrix();
        shader.setMat4("projection", projection);
        shader.setMat4("view", view);
        model = glm::translate(model, glm::vec3(-1.0f, 0.0f, -1.0f));
        shader.setMat4("model", model);
        glBindVertexArray(sphereVAO);
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_2D, sphereTexture);
        
        glDrawArrays(GL_TRIANGLES, 0, X_SEGMENTS* Y_SEGMENTS * 6);
        // glDrawElements(GL_TRIANGLES, X_SEGMENTS * Y_SEGMENTS * 6, GL_UNSIGNED_INT, 0);

        model = glm::mat4(1.0f);
        model = glm::translate(model, glm::vec3(2.0f, 0.0f, 0.0f));
        shader.setMat4("model", model);
        // glDrawElements(GL_TRIANGLES, X_SEGMENTS * Y_SEGMENTS * 6, GL_UNSIGNED_INT, 0);
        glDrawArrays(GL_TRIANGLES, 0, X_SEGMENTS * Y_SEGMENTS * 6);

        // draw skybox as last
        glDepthFunc(GL_LEQUAL);
        skyboxShader.use();
        view = glm::mat4(glm::mat3(camera.GetViewMatrix()));
        skyboxShader.setMat4("view", view);
        skyboxShader.setMat4("projection", projection);
        // skybox cube
        glBindVertexArray(skyboxVAO);
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_CUBE_MAP, cubemapTexture);
        glDrawArrays(GL_TRIANGLES, 0, 36);
        glBindVertexArray(0);
        glDepthFunc(GL_LESS);

        // glfw: swap buffers and poll IO events (keys pressed/released, mouse moved etc.)
        // -------------------------------------------------------------------------------
        glfwSwapBuffers(window);
        glfwPollEvents();
    }

    // optional: de-allocate all resources once they've outlived their purpose:
    // ------------------------------------------------------------------------
    glDeleteVertexArrays(1, &skyboxVAO);
    glDeleteBuffers(1, &skyboxVBO);

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