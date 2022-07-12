#version 330
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D texture1;

void main(){
    FragColor = texture(texture1, TexCoords);
    // FragColor = vec4(0.94, 0.06, 0.21, 1);
}