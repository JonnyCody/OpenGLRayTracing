---
typora-copy-images-to: .asset
---

# OpenGL光线追踪

## 1.0 前言

本项目渲染的是静态场景，加入了控制镜头环节，所以可以移动镜头看到场景的不同角度。

## 1.1 效果展示

### 1.1.1 ray tracing in one week的最终场景

![image-20220814113535210](https://github.com/JonnyCody/OpenGLRayTracing/tree/master/.asset/image-20220814113535210.png)

### 1.1.4 加入BVH加速结构前后对比

#### 没有加入BVH

![没有加入BVH](.asset\没有加入BVH.gif)

#### 加入BVH

![加入BVH](.asset\加入BVH.gif)

通过两张动图对比，可以发现使用BVH后，镜头移动卡顿减少，在球体少的部分，明显更加丝滑。

### 1.1.2 导入石头模型

![image-20220814133327701](.asset\image-20220814133327701.png)

### 1.1.3 Cornell Box

Cornell box不知为何墙壁和地面的阴影被消除了，从球的反光中可以明显看出地面是有立方体的阴影。

![image-20220814134245565](.asset\image-20220814134245565.png)

## 1.2 项目前期依赖

### 1.2.1 OpenGL

本项目是使用《Learn OpenGL》<sup>[1]</sup>的源码作为基础，中文翻译网址为[LearnOpenGL CN (learnopengl-cn.github.io)](https://learnopengl-cn.github.io/)

本项目的编译，链接库都可以在《Learn OpenGL》中找到。

### 1.2.2 Ray tracing系列

本项目光线追踪部分主要参考[ray tracing in one week](https://raytracing.github.io/books/RayTracingInOneWeekend.html)<sup>[2]</sup>进行更改，BVH部分参考[ray tracing: the next week](https://raytracing.github.io/books/RayTracingTheNextWeek.html#boundingvolumehierarchies)<sup>[3]</sup>。

### 1.2.3 基于OpenGL的GPU光线追踪

知乎大佬Ubp.a实现了基于[OpenGL的GPU光线追踪]([基于OpenGL的GPU光线追踪 - 知乎 (zhihu.com)](https://zhuanlan.zhihu.com/p/51387524))<sup>[4]</sup>，当时想法是看懂大佬的源码，然后自己仿造一个，反复看了多次，无法理解入栈出栈的细节实现，但是大佬给我提供了优化BVH的思路，使用texture存储数据，完成GPU和CPU的数据传输。

### 1.2.4 一周末内学会GLSL光线追踪

连冠荣大佬使用GLSL实现了[2]的主要部分，为具体实现OpenGL光追提供了思路。

## 1.3 实现BVH过程

### 1.3.1 GPU实现BVH

#### 难点：

- GPU无法运行递归函数，无法使用指针实现二叉树，所以采用数组实现二叉树
- BVH的实现类似于二叉树的前序遍历，数组实现的二叉树无法一个阶段完成BVH的构造

#### 解决方法：

- 首先将BVH构造的过程分为两个阶段，第一阶段是根据物体的包围盒进行排序，排序过程如下

  ```glsl
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
  ```

  实现过程仿照BVH构造的排序过程，首先总体排序，然后进行二分排序，排序的一句是物体包围盒的某个坐标轴，因为无法使用递归，所以`SortAABB(world, start, start + span, axis);`函数采用的是冒泡排序，计算量较大。

- 构建二叉树阶段，与普通二叉树构造过程不同，本次构造二叉树的构造是从叶子节点进行构造，最后完成根节点，因为物体数量是已知的假设为n，所以二叉树的总节点数量是2n-1。

  ```glsl
  struct BVHNode
  {
  	AABB aabb;
  	int children[2];
  	int parent;
  	int objectIndex;
  };
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
  ```

  构建过程：

  假设有4个物体，分别为0 1 2 3，则父节点是从4开始构建，物体0和1的父节点为4，物体2和3的父节点为4，进而迭代构建，直到节点数达到7。

  ![image-20220814162152001](.asset\image-20220814162152001.png)

#### 缺陷：

该方法使用迭代和数组实现了BVH的构造，但是给GPU带来了很大的计算量，首先需要对物体进行排序，采用的是O(n<sup>2</sup>)的冒泡排序，最终运行结果是使用了BVH效果比不使用BVH效果更加差。所以需要对该方法进行改进，改进的措施就是使用CPU构建BVH，GPU直接使用BVH。

### 1.3.2 CPU实现BVH

C++既可以使用递归，也提供了排序函数，再加上lambda函数，可以很方便的实现BVH，但受制于GPU，无法像[3]中chapter7中那样实现BVH，因为GPU无法读取，所以CPU构造BVH的主要思路也是1.3.3部分，先排序，然后构造BVH。

该部分的难点是实现数据的沟通，类似于定义协议。首先定义两个samplerBuffer，分别由于存储物体数据和BVH节点（obj模型暂不考虑），samplerBuffer定义为GL_RGBA32F，这样一个通道可以存放四个float数据。物体和BVH都是以3个通道为单位，其中数据定义如下

![image-20220814165644819](.asset\image-20220814165644819.png)



## 1.4 代码结构

主要介绍src部分

- include：
  - ray_tracing：对物体、材料、包围盒进行定义

- src
  - cornell_box：对cornell_box进行测试，与其他部分主要区别在于发光材料的处理，因为cornell box只有顶部发光，没有周围的自然光，可能是GPU的优化设置，导致墙壁天花板地面部分没有阴影，但是立方体和球体还是有阴影的
  - model_load：主要对模型导入进行测试，结果表明对于简单模型可以显示，对于复杂的模型运行过慢，后续计划对模型的三角形也建立BVH，进行优化。
  - ray_tacing_in_one_weekend：这部分引用[5]，将ray_tracing_in_one_weekend的C++代码翻译成glsl
  - ray_tracing_optimize：对BVH的构建过程进行优化，将构建过程转移到CPU部分，然后加入了obj模型和天空盒部分，是最终各种功能集成的部分，但是对于cornell_box的发光材料部分没有进行处理
  - ray_tracing_the_next_week：在GPU部分实现了BVH的构建，在[3]中第三章介绍BVH，所以文件命名为BVH
  - sky_box：该部分没有光追，当初实现的想法是先构建一个球，然后加入光追，所以该部分只有天空盒和多个三角形构成的球体，球体有贴图和反射折射功能
  - sky_box_ray_tracing：对光追部分加入天空盒的测试

## 1.5 项目待改进点

- 天空盒不清晰，可能是因为采样的方向不够密集
- 对于复杂模型，则会过慢，后续需要对模型按照三角形进行处理，主要前期考虑到一个三角形需要占到8个通道，而其他物体只需要3个通道，所以对模型直接处理，没有进行拆分
- 工程代码需要按照CMake进行编译，没有系统学习过CMake，但这类工程还是使用Cmake编译比较方便

## 参考资料

[1] De Vries J. Learn opengl[J]. Licensed under CC BY, 2015, 4.

[2] Shirley P. Ray tracing in one weekend[J]. Amazon Digital Services LLC, 2018, 1.

[3] Shirley P. Ray Tracing: The Next Week[J]. United States of America: Kindle, 2016.

[4] [基于OpenGL的GPU光线追踪 - 知乎 (zhihu.com)](https://zhuanlan.zhihu.com/p/51387524)

[5] [(光追渲染) 一周末内学会GLSL光线追踪 - 知乎 (zhihu.com)](https://zhuanlan.zhihu.com/p/321878263)