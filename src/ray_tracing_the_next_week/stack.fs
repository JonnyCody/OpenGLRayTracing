#version 330 core

int stack[10];
int stackTop = -1;

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