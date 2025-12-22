#version 430

struct Particle
{
    vec3 position;
    vec3 velocity;
    ivec2 pixel;
    vec4 color;
};

layout(std430, binding = 1) buffer OutputBuffer {
    Particle particles[];
};

uniform mat4 projection;

layout(location=0) out vec3 vColor;

void main()
{
    uint id = gl_VertexID;
    vec2 pos2d = vec2(particles[id].position.x, particles[id].position.z);
    gl_Position = projection * vec4(pos2d, 0.0, 1.0);
    gl_PointSize = 5.0;
    vColor = particles[id].color.rgb; 
}