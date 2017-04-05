#define D2D_INPUT_COUNT 2
#define D2D_INPUT0_SIMPLE
#define D2D_INPUT1_SIMPLE

#include "d2d1effecthelpers.hlsli"

float opacity;
float feather = 0.1;

D2D_PS_ENTRY(main)
{
    float4 color = D2DGetInput(0);
    float mask = D2DGetInput(1).r;

	float alpha;
	if (opacity >= mask)
	{
		alpha = 1.0;
	}
	else if (opacity + feather - mask >= 0)
	{
		alpha = 0.0;
	}
	else
	{
		alpha = saturate((opacity - mask) / feather);
	}
	
    return color * alpha;
}