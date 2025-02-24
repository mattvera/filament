layout(location = 0) out vec4 fragColor;

#if defined(MATERIAL_HAS_POST_LIGHTING_COLOR)
void blendPostLightingColor(const MaterialInputs material, inout vec4 color) {
#if defined(POST_LIGHTING_BLEND_MODE_OPAQUE)
    color = material.postLightingColor;
#elif defined(POST_LIGHTING_BLEND_MODE_TRANSPARENT)
    color = material.postLightingColor + color * (1.0 - material.postLightingColor.a);
#elif defined(POST_LIGHTING_BLEND_MODE_ADD)
    color += material.postLightingColor;
#elif defined(POST_LIGHTING_BLEND_MODE_MULTIPLY)
    color *= material.postLightingColor;
#elif defined(POST_LIGHTING_BLEND_MODE_SCREEN)
    color += material.postLightingColor * (1.0 - color);
#endif
}
#endif

void main() {
    filament_lodBias = frameUniforms.lodBias;

    // See shading_parameters.fs
    // Computes global variables we need to evaluate material and lighting
    computeShadingParams();

    // Initialize the inputs to sensible default values, see material_inputs.fs
    MaterialInputs inputs;
    initMaterial(inputs);

    // Invoke user code
    material(inputs);

    fragColor = evaluateMaterial(inputs);

#if defined(TARGET_METAL_ENVIRONMENT)
    // Hack to force spirv-cross to keep the light_structure texture present in the argument buffer.
    float hack = textureLod(light_structure, vec2(0.0, 0.0), 0.0).r;
    fragColor.x += hack;
    fragColor.x -= hack;
#endif

#if defined(VARIANT_HAS_DIRECTIONAL_LIGHTING) && defined(VARIANT_HAS_SHADOWING)
    bool visualizeCascades = bool(frameUniforms.cascades & 0x10u);
    if (visualizeCascades) {
        fragColor.rgb *= uintToColorDebug(getShadowCascade());
    }
#endif

#if defined(VARIANT_HAS_FOG)
    vec3 view = getWorldPosition() - getWorldCameraPosition();
    fragColor = fog(fragColor, view);
#endif

#if defined(MATERIAL_HAS_POST_LIGHTING_COLOR) && !defined(MATERIAL_HAS_REFLECTIONS)
    blendPostLightingColor(inputs, fragColor);
#endif
}
