Shader "Hidden/CloudShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        LOD 100
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDUnlitShader" }

        Pass
        {   
            
            Name "CLOUDS"
            Tags {"LightMode" = "Forward"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.5
            #include "UnityCG.cginc" 
            #include "Assets/Scripts/NoiseGen/Noise.compute"

            struct appdata {
                float4 vertPos : POSITION;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(1)
                float3 worldPos : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            CBUFFER_START(CloudSettings)
                //Bounds
                float3 MinBounds;
                float3 MaxBounds;

                //Textures
                sampler3D ShapeTex;
                float ShapeScale;
                sampler3D DetailTex;
                float DetailScale;
                sampler2D NoiseTex;

                //Weather
                fixed3 CloudGrad;

                //Raymarching
                fixed Coverage;
                fixed ErodeStrength;
                float StepSize;
                float CheapScale;
                float StepRandomness;

                //Rendering
                float3 DensityMods;
                float Scattering;
            CBUFFER_END

            CBUFFER_START(ChangesPerFrame)
                fixed4 LightColor = 1;
                float3 LightDir;
                float3 MainOffset;
                float3 DetailOffset;
            CBUFFER_END

            fixed invLerp(float a, float b, float t)
            {
                return (t - a) / (b - a);
            }
            fixed boxGrad(fixed center, fixed plateauSize, fixed steepness, fixed x)
            {
                return saturate((1 - abs(x - center) * steepness) + plateauSize);
            }
             /*
                -1 <= g <= 1
                -1 is back-scattering 
                1 is forward-scattering
                0 is isotropic scattering
            */
            #define Four_PI 12.5663
            fixed henyeyGreenstein(fixed cosA, fixed g)
            {
                float g2 = g * g;
                return (1 - g2) / (Four_PI * pow(1 + g2 - 2 * g * cosA, 1.5));
            }
            float rayBoxDist(float3 origin, float3 dir, float3 boundsMin, float3 boundsMax)
            {
                float3 t0 = (boundsMin - origin) / dir;
                float3 t1 = (boundsMax - origin) / dir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);

                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(min(tmax.y, tmax.z), tmax.x);

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);

                return dstInsideBox;
            }
            fixed beerPowder(fixed density)
            {
                /*
                    To summarize this combines cloud light calculations:
                        Beer's Law - describes of absortion of energy through matter (in this case, light through clouds)
                        Powdered Sugar Effect - creates the dark areas in clouds that seem unusual and are easy to miss + can be seen in powdered sugar
                        Henyey-Greenstein - anisotropic scattering
                */
                float negDensity = -density * DensityMods.x;
                fixed beer = exp(negDensity);
                fixed powder = 1 - exp(negDensity * 2);
                return beer * powder;
            }
            #define ONE_THIRD 1/3
            #define TWO_THIRDS 2/3
            fixed sampleShape(float3 UVW, float3 minBounds, float3 maxBounds, bool cheap = false)
            {
                float3 samplePos = (UVW + MainOffset) / ShapeScale;

                fixed heightGrad = invLerp(minBounds.y, maxBounds.y, UVW.y);
                fixed cloudTypeGrad = boxGrad(CloudGrad.x, CloudGrad.y, CloudGrad.z, heightGrad);

                fixed4 noiseColor = cheap ? tex3Dlod(ShapeTex, float4(samplePos, 1)) : tex3Dlod(ShapeTex, float4(samplePos, 0));
                fixed noiseCombine = noiseColor.r * noiseColor.g * noiseColor.b * noiseColor.a;

                return noiseCombine * heightGrad * cloudTypeGrad * Coverage;
            }
            fixed sampleDetail(float3 UVW)
            {
                fixed4 noiseColor = tex3Dlod(DetailTex, float4((UVW + DetailOffset) / DetailScale, 0));
                fixed noiseCombine = noiseColor.r * noiseColor.g * noiseColor.b;
                return noiseCombine * ErodeStrength;
            }
            float lightMarch(float3 rayOrigin, float3 lightDir, float3 minBounds, float3 maxBounds, uint steps = 5, fixed distPercent = .4)
            {
                float boxDst = rayBoxDist(rayOrigin, -lightDir, minBounds, maxBounds);
                float stepSize = (boxDst * distPercent) / (steps - 1);
                float density = 0;
                float3 samplePos = rayOrigin;
                for (uint step = 0; step < steps; step++)
                {
                    density += sampleShape(rayOrigin, minBounds, maxBounds, true) * stepSize;
                    samplePos += -lightDir * stepSize;
                }

                float finalStepSize = (boxDst - steps * stepSize) * .75;
                samplePos += finalStepSize * -lightDir;
                density += sampleShape(samplePos, minBounds, maxBounds, true) * finalStepSize;
                return density;
            }
            
            v2f vert(appdata IN) {
                v2f OUT;
                OUT.pos = UnityObjectToClipPos(IN.vertPos);
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertPos);
                OUT.screenPos = ComputeScreenPos(OUT.pos);
                UNITY_TRANSFER_FOG(OUT, OUT.vertex);
                return OUT;
            }
            fixed4 frag(v2f IN) : SV_Target {
                fixed4 OUT;

                //Convert bounds to world space
                const float3 minBounds = mul(unity_ObjectToWorld, float4(MinBounds, 1));
                const float3 maxBounds = mul(unity_ObjectToWorld, float4(MaxBounds, 1));

                //Get direction from camera to pixel
                float3 posFromCam = IN.worldPos - _WorldSpaceCameraPos;
                float3 viewDir = normalize(IN.worldPos - _WorldSpaceCameraPos);

                //Get distance from each end of the AABB bounds so you can take a certain amount of steps
                float boxDst = rayBoxDist(_WorldSpaceCameraPos, viewDir, minBounds, maxBounds);
                float stepSize = StepSize * CheapScale;
                uint steps = ceil(abs(boxDst / stepSize));

                float phaseVal = saturate(henyeyGreenstein(dot(viewDir, -LightDir), Scattering) * DensityMods.y * LightColor.a);

                fixed density = 0;
                fixed energy = 0;
                float3 samplePos = IN.worldPos;
                //Did you hit cloud surface?
                bool hitSurface;
                //Raymarch
                [loop]
                for (uint step; step < steps; step++)
                {
                    float3 randSamplePos = samplePos + neg2PosRange(tex2Dlod(NoiseTex, float4(samplePos.xz / ShapeScale, 0, 0))) * StepRandomness * stepSize * viewDir;
                    fixed shape = sampleShape(randSamplePos, minBounds, maxBounds, !hitSurface);
                    if (shape >= 0.001)
                    {
                        if (!hitSurface)
                        {
                            hitSurface = true;
                            //Move back
                            samplePos -= stepSize;
                            //Reduce step size
                            stepSize /= CheapScale;
                            //Double steps
                            steps = ceil(steps * CheapScale);
                        }

                        fixed pointDensity = shape - sampleDetail(randSamplePos) * (1 - shape);
                        density += pointDensity;
                        energy += beerPowder(lightMarch(randSamplePos, LightDir, minBounds, maxBounds)) * phaseVal;

                        if (density >= 1)
                            break;
                    }

                    //Increase sample point by amount
                    samplePos += stepSize * viewDir;
                }

                OUT.rgb = energy * LightColor * LightColor.a;
                OUT.a = saturate(density);
                UNITY_APPLY_FOG(IN.fogCoord, OUT);
                return OUT;
            }
            ENDCG
        }

        Pass
        {
            Tags {"LightMode" = "ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
