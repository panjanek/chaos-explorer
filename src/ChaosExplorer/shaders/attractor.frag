#version 430

layout(location=0) in vec3 vColor;
out vec4 outputColor;

void main()
{
    vec2 uv = gl_PointCoord * 2.0 - 1.0; 
    float r = length(uv); 
    if (r > 1.0)
        discard;

    float alpha = smoothstep(1.0, 0.0, r);
    alpha = alpha;
    outputColor = vec4(vColor*alpha, alpha*0.5);
}