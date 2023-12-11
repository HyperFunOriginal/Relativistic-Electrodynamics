
// Finite Difference Coefficients; 6th order
static const float cdelO6[49] = { -2.45000000, 6.00000000, -7.50000000, 6.66666667, -3.75000000, 1.20000000, -0.16666667, -0.16666667, -1.28333333, 2.50000000, -1.66666667, 0.83333333, -0.25000000, 0.03333333, 0.03333333, -0.40000000, -0.58333333, 1.33333333, -0.50000000, 0.13333333, -0.01666667, -0.01666667, 0.15000000, -0.75000000, 0.00000000, 0.75000000, -0.15000000, 0.01666667, 0.01666667, -0.13333333, 0.50000000, -1.33333333, 0.58333333, 0.40000000, -0.03333333, -0.03333333, 0.25000000, -0.83333333, 1.66666667, -2.50000000, 1.28333333, 0.16666667, 0.16666667, -1.20000000, 3.75000000, -6.66666667, 7.50000000, -6.00000000, 2.45000000 };

static const float cdel6O2[7] = { 1, -6, 15, -20, 15, -6, 1 };

int3 resolution;
float lengthScale;
float timestep;

// Forward Transform (Coordinate space to Physical space)
float3 CoordinateTransform(float3 ids)
{
    ids = (ids + .5) / resolution * 2. - 1.;
    return log((1. + ids) / (1. - ids)) * .25 * resolution * lengthScale;
}

// Backwards Transform (Physical space to Coordinate space)
float3 InverseCoordinateTransform(float3 pos)
{
    pos *= 2. / (resolution * lengthScale);
    return clamp(round((tanh(pos) + 1.) * .5 * resolution - .5), 0, resolution - 1);
}

// Finite Differencing
static float FirstDerivative(int i, int pos, int resolution)
{
    float scale = float(pos + .5) / resolution * 2. - 1.;
    pos -= clamp(pos - 3, 0, resolution - 7);
    return cdelO6[clamp(i + 3, 0, 6) + pos * 7] * (1. - scale * scale);
}

float LorentzGamma(float3 vel3)
{
    return rsqrt(1 - dot(vel3, vel3));
}

// Faraday Tensor from E^i and B^i
float4x4 FaradayTensor(float3 E, float3 B)
{
    return float4x4(0, E.x, E.y, E.z, -E.x, 0, -B.z, B.y, -E.y, B.z, 0, -B.x, -E.z, -B.y, B.x, 0);
}

// int3 to int indexing
int Idx(int3 index)
{
    index = clamp(index, 0, resolution - 1);
    return index.z * resolution.y * resolution.x + index.y * resolution.x + index.x;
}

// Checks if voxel is no greater than 12 voxels to boundary
int FarBorder(int3 index)
{
    index -= clamp(index, 11, resolution - 12);
    return index.x != 0 || index.y != 0 || index.z != 0;
}

// Checks if voxel is no greater than 7 voxels to boundary
int Border(int3 index)
{
    index -= clamp(index, 6, resolution - 7);
    return index.x != 0 || index.y != 0 || index.z != 0;
}

// int3 to int indexing; Sends out-of-range indices to -1
int IdxEx(int3 index)
{
    int3 del = index - clamp(index, 0, resolution - 1);
    if (dot(del, del) != 0)
        return -1;
    return index.z * resolution.y * resolution.x + index.y * resolution.x + index.x;
}

// Obsolete; Forward Coordinate Transform
float3 YieldPosition(int3 coords)
{
    return CoordinateTransform(coords);
}

// Kreiss Oliger Dissipation for scalar fields
float KreissOliger(RWStructuredBuffer<float> field, int3 position)
{
    float3 scale = float3(position + .5) / resolution * 2. - 1.;
    scale = 1. - scale * scale;
    
    float result = 0;
    for (int i = -3; i < 4; i++)
    {
        float coeff = cdel6O2[i + 3];
        result += field[Idx(position + int3(i, 0, 0))] * coeff * scale.x;
        result += field[Idx(position + int3(0, i, 0))] * coeff * scale.y;
        result += field[Idx(position + int3(0, 0, i))] * coeff * scale.z;
    }
    return result * 0.015625;
}

// Kreiss Oliger Dissipation for 3-vector fields
float3 KreissOliger(RWStructuredBuffer<float3> field, int3 position)
{
    float3 scale = float3(position + .5) / resolution * 2. - 1.;
    scale = 1. - scale * scale;
    
    float3 result = 0;
    for (int i = -3; i < 4; i++)
    {
        float coeff = cdel6O2[i + 3];
        result += field[Idx(position + int3(i, 0, 0))] * coeff * scale.x;
        result += field[Idx(position + int3(0, i, 0))] * coeff * scale.y;
        result += field[Idx(position + int3(0, 0, i))] * coeff * scale.z;
    }
    return result * 0.015625;
}

// Kreiss Oliger Dissipation for 4-vector fields
float4 KreissOliger(RWStructuredBuffer<float4> field, int3 position)
{
    float3 scale = float3(position + .5) / resolution * 2. - 1.;
    scale = 1. - scale * scale;
    
    float4 result = 0;
    for (int i = -3; i < 4; i++)
    {
        float coeff = cdel6O2[i + 3];
        result += field[Idx(position + int3(i, 0, 0))] * coeff * scale.x;
        result += field[Idx(position + int3(0, i, 0))] * coeff * scale.y;
        result += field[Idx(position + int3(0, 0, i))] * coeff * scale.z;
    }
    return result * 0.015625;
}

// Spatial Divergence of 4-vector, D_i V^i
float DivSpatial(RWStructuredBuffer<float4> field, int3 position)
{
    float result = 0;
    int3 offset = clamp(position - 3, 0, resolution - 7) + 3;
    for (int i = -3; i < 4; i++)
    {
        result += field[Idx(offset + int3(i, 0, 0))].x * FirstDerivative(i, position.x, resolution.x);
        result += field[Idx(offset + int3(0, i, 0))].y * FirstDerivative(i, position.y, resolution.y);
        result += field[Idx(offset + int3(0, 0, i))].z * FirstDerivative(i, position.z, resolution.z);
    }
    return result / lengthScale;
}

// Spatial Derivative of 3-vector, D_i V^j
float3x3 Grad(RWStructuredBuffer<float3> field, int3 position)
{
    float3x3 result = 0;
    int3 offset = position - clamp(position - 3, 0, resolution - 7);
    for (int i = -3; i < 4; i++)
    {
        result[0] += field[Idx(int3(i + position.x - offset.x + 3, position.y, position.z))] * FirstDerivative(i, position.x, resolution.x);
        result[1] += field[Idx(int3(position.x, i + position.y - offset.y + 3, position.z))] * FirstDerivative(i, position.y, resolution.y);
        result[2] += field[Idx(int3(position.x, position.y, i + position.z - offset.z + 3))] * FirstDerivative(i, position.z, resolution.z);
    }
    return result / lengthScale;
}

// Spatial Derivative of 3-vector (readonly buffer), D_i V^j
float3x3 Grad(StructuredBuffer<float3> field, int3 position)
{
    float3x3 result = 0;
    int3 offset = clamp(position - 3, 0, resolution - 7) + 3;
    for (int i = -3; i < 4; i++)
    {
        result[0] += field[Idx(offset + int3(i, 0, 0))] * FirstDerivative(i, position.x, resolution.x);
        result[1] += field[Idx(offset + int3(0, i, 0))] * FirstDerivative(i, position.y, resolution.y);
        result[2] += field[Idx(offset + int3(0, 0, i))] * FirstDerivative(i, position.z, resolution.z);
    }
    return result / lengthScale;
}

// Curl from spatial derivatives, e^ij_k D_j V^k
float3 Curl(float3x3 dx)
{
    return -(cross(dx[0], float3(1, 0, 0)) + cross(dx[1], float3(0, 1, 0)) + cross(dx[2], float3(0, 0, 1)));
}

// Divergence from spatial derivatives, D_i V^i
float Div(float3x3 dx)
{
    return dx._11 + dx._22 + dx._33;
}

// Spatial Derivative of scalar field, D_i S 
float3 Grad(RWStructuredBuffer<float> field, int3 position)
{
    float3 result = 0;
    int3 offset = clamp(position - 3, 0, resolution - 7) + 3;
    for (int i = -3; i < 4; i++)
    {
        result[0] += field[Idx(offset + int3(i, 0, 0))] * FirstDerivative(i, position.x, resolution.x);
        result[1] += field[Idx(offset + int3(0, i, 0))] * FirstDerivative(i, position.y, resolution.y);
        result[2] += field[Idx(offset + int3(0, 0, i))] * FirstDerivative(i, position.z, resolution.z);
    }
    return result / lengthScale;
}

float3x3 MatrixInverse(float3x3 mat)
{
    // _11 _12 _13
    // _21 _22 _23
    // _31 _32 _33
    
    return transpose(float3x3(mat._22 * mat._33 - mat._23 * mat._32, mat._21 * mat._33 - mat._23 * mat._31, mat._21 * mat._32 - mat._22 * mat._31, mat._12 * mat._33 - mat._13 * mat._32, mat._11 * mat._33 - mat._31 * mat._13, mat._11 * mat._32 - mat._12 * mat._31, mat._12 * mat._23 - mat._13 * mat._22, mat._11 * mat._23 - mat._21 * mat._13, mat._11 * mat._22 - mat._21 * mat._12)) * float3x3(1, -1, 1, -1, 1, -1, 1, -1, 1) / determinant(mat);
}