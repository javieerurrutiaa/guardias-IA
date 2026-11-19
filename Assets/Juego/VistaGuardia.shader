Shader "UDIT/Guardia/VistaGuardia"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Entrada
            {
                float4 posicion : POSITION;
                fixed4 color : COLOR;
            };

            struct Interpolado
            {
                float4 posicion : SV_POSITION;
                fixed4 color : COLOR;
            };

            fixed4 _Color;

            Interpolado vert(Entrada entrada)
            {
                Interpolado salida;
                salida.posicion = UnityObjectToClipPos(entrada.posicion);
                salida.color = entrada.color * _Color;
                return salida;
            }

            fixed4 frag(Interpolado entrada) : SV_Target
            {
                return entrada.color;
            }
            ENDCG
        }
    }
}
