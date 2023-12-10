using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CMath
{
    public static class Miscellaneous
    {
        internal const string asciiBrightness = " `.-':_,^=;><+!rc*/z?sLTv)J7(|Fi{C}fI31tlu[neoZ5Yxjya]2ESwqkP6h9d4VpOGbUAKXHm8RD#$Bg0MNWQ%&@";
        public static string PadCenter(string text, int totalLength) => text.PadLeft((totalLength + text.Length) >> 1).PadRight(totalLength);
    }
    public struct VectorND
    {
        internal readonly List<double> components;
        public int dimension => components.Count;
        public double sqMagnitude
        {
            get
            {
                double final = 0d;
                foreach (double a in components)
                    final += a * a;
                return final;
            }
        }
        public double sqMagnitudeSmooth
        {
            get
            {
                double final = 1E-280d;
                foreach (double a in components)
                    final += a * a;
                return final;
            }
        }
        public double magnitude => Math.Sqrt(sqMagnitude);
        public double magnitudeSmooth => Math.Sqrt(sqMagnitudeSmooth);
        public VectorND normalized => this / magnitude;
        public VectorND normalizedNonDegenerate => this / magnitudeSmooth;
        internal int printLength
        {
            get
            {
                double max = 0d;
                for (int i = 0; i < dimension; i++)
                    max = Math.Max(max, Math.Abs(this[i]));
                if (!double.IsFinite(max))
                    return 2;
                return (int)Math.Log10(max + 1);
            }
        }

        public override string ToString() => ToString(4);
        public string ToString(int digits)
        {
            string s = "[";
            for (int i = 0; i < dimension; i++)
                s += this[i].ToString("f" + digits).PadLeft(4 + digits) + ((i < dimension - 1) ? ", " : "  ]");
            return s;
        }

        public static VectorND zero => new VectorND(0);
        public static VectorND one => new VectorND(1);

        public VectorND Evaluate(Func<double, double> f)
        {
            VectorND result = new VectorND(0);
            for (int i = 0; i < dimension; i++)
                result[i] = f(this[i]);
            return result;
        }
        public static VectorND FromSphericalCoords(double radius, params double[] angles)
        {
            VectorND final = new VectorND(0);
            double product = radius;
            for (int i = 0; i < angles.Length; i++)
            {
                final[i] = Math.Cos(angles[i]) * product;
                product *= Math.Sin(angles[i]);
            }
            final[angles.Length] = product;
            return final;
        }
        public static VectorND FromSphericalCoords(double radius, VectorND angles)
        {
            VectorND final = new VectorND(0);
            double product = radius;
            for (int i = 0; i < angles.dimension; i++)
            {
                final[i] = Math.Cos(angles[i]) * product;
                product *= Math.Sin(angles[i]);
            }
            final[angles.dimension] = product;
            return final;
        }
        public static double Dot(VectorND a, VectorND b)
        {
            double total = 0d;
            for (int i = 0; i < Math.Max(a.dimension, b.dimension); i++)
                total += a[i] * b[i];
            return total;
        }
        public static VectorND Project(VectorND a, VectorND onto) => onto * Dot(a, onto) / onto.sqMagnitudeSmooth;
        public static List<VectorND> Orthonormalize(List<VectorND> n)
        {
            List<VectorND> a = new List<VectorND>(n);
            int maxDimension = a[0].dimension;
            for (int i = 1; i < a.Count; i++)
                if (a[i].dimension > maxDimension)
                    maxDimension = a[i].dimension;
            if (a.Count > maxDimension)
                a.RemoveRange(maxDimension, a.Count - maxDimension);
            while (a.Count < maxDimension)
            {
                double[] arr = new double[maxDimension];
                arr[a.Count] = 1d;
                a.Add(new VectorND(arr));
            }
            List<VectorND> final = new List<VectorND>(maxDimension);
            final.Add(a[0].normalizedNonDegenerate);
            for (int i = 1; i < maxDimension; i++)
            {
                final.Add(a[i]);
                for (int j = 0; j < i; j++)
                    final[i] -= Project(a[i], final[j]);
                final[i] = final[i].normalizedNonDegenerate;
            }
            return final;
        }
        public static VectorND Lerp(VectorND a, VectorND b, double t) => a + (b - a) * t;
        public static double Angle(VectorND a, VectorND b) => Math.Acos(Dot(a.normalizedNonDegenerate, b.normalizedNonDegenerate));
        public static Matrix OuterProduct(VectorND a, VectorND b)
        {
            Matrix result = new Matrix(Math.Max(a.dimension, b.dimension));
            for (int i = 0; i < a.dimension; i++)
                for (int j = 0; j < b.dimension; j++)
                    result[i, j] = a[i] * b[j];
            return result;
        }

        public VectorND(VectorND original) => components = new List<double>(original.components.ToArray());
        public VectorND(params double[] vector) => components = new List<double>(vector);
        public static explicit operator VectorND(List<double> a) => new VectorND(a.ToArray());
        public static VectorND operator +(VectorND a, VectorND b)
        {
            VectorND final = new VectorND(0);
            for (int i = 0; i < Math.Max(a.dimension, b.dimension); i++)
                final[i] = a[i] + b[i];
            return final;
        }
        public static VectorND operator -(VectorND a)
        {
            VectorND neg = new VectorND(0);
            for (int i = 0; i < a.components.Count; i++)
                neg[i] = -a[i];
            return neg;
        }
        public static VectorND operator -(VectorND a, VectorND b)
        {
            VectorND final = new VectorND(0);
            for (int i = 0; i < Math.Max(a.dimension, b.dimension); i++)
                final[i] = a[i] - b[i];
            return final;
        }
        public static VectorND operator *(VectorND a, VectorND b)
        {
            VectorND final = new VectorND(0);
            for (int i = 0; i < Math.Max(a.dimension, b.dimension); i++)
                final[i] = a[i] * b[i];
            return final;
        }
        public static VectorND operator /(VectorND a, VectorND b)
        {
            VectorND final = new VectorND(0);
            for (int i = 0; i < Math.Max(a.dimension, b.dimension); i++)
                final[i] = a[i] / b[i];
            return final;
        }
        public static VectorND operator *(VectorND a, double b)
        {
            VectorND final = new VectorND(a.components.ToArray());
            for (int i = 0; i < final.dimension; i++)
                final[i] *= b;
            return final;
        }
        public static VectorND operator *(double b, VectorND a) => a * b;
        public static VectorND operator /(VectorND a, double b)
        {
            VectorND final = new VectorND(a.components.ToArray());
            for (int i = 0; i < final.dimension; i++)
                final[i] /= b;
            return final;
        }
        internal static VectorND DivRnd(double b, VectorND a)
        {
            VectorND final = new VectorND(a.components.ToArray());
            for (int i = 0; i < final.dimension; i++)
                final[i] = (Math.Abs(final[i]) < 1E-20d) ? 0d : b / final[i];
            return final;
        }
        public double this[int a]
        {
            get
            {
                if (components == null)
                    throw new NullReferenceException("Vector has not been initialised.");
                if (a >= components.Count || a < 0)
                    return 0d;
                return components[a];
            }
            set
            {
                if (components == null)
                    throw new NullReferenceException("Vector has not been initialised.");
                if (a < 0)
                    throw new IndexOutOfRangeException("Index cannot be negative.");
                while (a >= components.Count)
                    components.Add(0);
                components[a] = value;
            }
        }
    }
    public struct Matrix
    {
        public readonly VectorND[] basis;
        public readonly int dimension;
        public double this[int i, int j]
        {
            get
            {
                return basis[i][j];
            }
            set
            {
                VectorND vec = basis[i];
                vec[j] = value;
                basis[i] = vec;
            }
        }

        public double vectorizedSqLength
        {
            get
            {
                double result = 0d;
                for (int i = 0; i < dimension; i++)
                    result += basis[i].sqMagnitude;
                return result;
            }
        }
        public double determinant
        {
            get
            {
                Matrix rowEch = ReduceRowEchelon(out Matrix _0, out double _1);
                for (int i = 0; i < dimension; i++)
                    _1 *= rowEch[i, i];
                return _1;
            }
        }
        public Matrix inverse
        {
            get
            {
                if (dimension == 2)
                    return new Matrix(2, new VectorND(this[1, 1], -this[0, 1]), new VectorND(-this[1, 0], this[0, 0])) * (1d / (this[1, 1] * this[0, 0] - this[1, 0] * this[0, 1]));

                Matrix temp = Transpose(ReduceRowEchelon(out Matrix R1, out double _0));
                temp = temp.ReduceRowEchelon(out Matrix R2, out _0);
                return Mul(Transpose(R2), Mul(DivId(1d, temp), R1));
            }
            set
            {
                this = value.inverse;
            }
        }
        public Matrix Minor(int x, int y)
        {
            Matrix matrix = new Matrix(dimension - 1);
            for (int i = 0, u = 0; i < dimension; i++)
            {
                if (i == x)
                    continue;
                for (int j = 0, v = 0; j < dimension; j++)
                {
                    if (j == y)
                        continue;
                    matrix[u, v] = this[i, j];
                    v++;
                }
                u++;
            }
            return matrix;
        }
        public Matrix ReduceRowEchelon(out Matrix transform, out double scalarMul)
        {
            scalarMul = 1d;
            transform = Identity(dimension);
            Matrix temp = new Matrix(dimension, basis);

            for (int i = 0; i < dimension - 1; i++) // Outer Column
            {
                while (Math.Abs(temp[i, i]) < 1E-20d) // Swap Rows
                {
                    double max = 0d; int maxIndex = 0;
                    for (int j = i + 1; j < dimension; j++) // Check Row
                    {
                        double val = Math.Abs(temp[i, j]);
                        if (val > max)
                        {
                            max = val;
                            maxIndex = j;
                        }
                    }

                    if (max < 1E-20d)
                        break;

                    for (int x = 0; x < dimension; x++) // Inner Column
                    {
                        double temp1 = temp[x, maxIndex];
                        double temp2 = transform[x, maxIndex];
                        temp[x, maxIndex] = temp[x, i];
                        transform[x, maxIndex] = transform[x, i];
                        temp[x, i] = temp1;
                        transform[x, i] = temp2;
                    }
                    scalarMul *= -1;
                }
                VectorND columnMul = VectorND.DivRnd(temp[i, i], temp.basis[i]);

                for (int j = i + 1; j < dimension; j++) // Row
                {
                    if (columnMul[j] == 0)
                        continue;

                    scalarMul /= columnMul[j];
                    for (int x = 0; x < dimension; x++) // Inner Column
                    {
                        temp[x, j] *= columnMul[j];
                        transform[x, j] *= columnMul[j];
                        temp[x, j] -= temp[x, i];
                        transform[x, j] -= transform[x, i];
                    }
                }
            }
            return temp;
        }
        public VectorND vectorization
        {
            get
            {
                VectorND result = new VectorND(basis[0]);
                for (int i = 1; i < dimension; i++)
                    result.components.AddRange(basis[i].components);
                return result;
            }
            set
            {
                for (int i = 0; i < dimension; i++)
                    for (int j = 0; j < dimension; j++)
                        this[i, j] = value[i * dimension + j];
            }
        }

        public VectorND Mul(VectorND vec)
        {
            VectorND result = new VectorND(new double[dimension]);
            for (int i = 0; i < dimension; i++)
                result += basis[i] * vec[i];
            return result;
        }
        public static Matrix Mul(Matrix a, Matrix b)
        {
            Matrix result = new Matrix(a.dimension);
            for (int i = 0; i < a.dimension; i++)
                result.basis[i] = a.Mul(b.basis[i]);
            return result;
        }
        public override string ToString() => ToString(3);
        public string ToString(int digits)
        {
            string result = "";

            int[] padding = new int[dimension];
            for (int i = 0; i < padding.Length; i++)
                padding[i] = basis[i].printLength;

            for (int i = 0; i < dimension; i++)
            {
                result += "[ ";
                for (int j = 0; j < dimension; j++)
                    result += Miscellaneous.PadCenter(basis[j][i].ToString("f" + digits), 2 + digits * 2 + padding[j]) + (j != dimension - 1 ? "" : "]");
                if (i != dimension - 1)
                    result += "\n";
            }
            return result;
        }

        internal static Matrix DivId(double b, Matrix a)
        {
            Matrix result = new Matrix(a.dimension);
            for (int i = 0; i < a.dimension; i++)
                result[i, i] = b / a[i, i];
            return result;
        }
        public static Matrix operator *(Matrix a, double b)
        {
            Matrix result = new Matrix(a.dimension);
            for (int i = 0; i < a.dimension; i++)
                result.basis[i] = a.basis[i] * b;
            return result;
        }
        public static Matrix operator +(Matrix a, Matrix b)
        {
            Matrix result = new Matrix(a.dimension);
            for (int i = 0; i < a.dimension; i++)
                result.basis[i] = a.basis[i] + b.basis[i];
            return result;
        }
        public static Matrix operator -(Matrix a)
        {
            Matrix result = new Matrix(a.dimension);
            for (int i = 0; i < a.dimension; i++)
                result.basis[i] = -a.basis[i];
            return result;
        }
        public static Matrix operator -(Matrix a, Matrix b) => a + (-b);

        public static Matrix GramSchmidt(Matrix input)
        {
            Matrix result = new Matrix(input.dimension, input.basis);
            for (int i = 0; i < result.dimension; i++)
            {
                for (int j = 0; j < i; j++)
                    result.basis[i] -= VectorND.Dot(result.basis[i], result.basis[j]) * result.basis[j];
                result.basis[i] /= result.basis[i].magnitudeSmooth;
            }
            return result;
        }
        public static Matrix Transpose(Matrix input)
        {
            Matrix mat = new Matrix(input.dimension);
            for (int i = 0; i < input.dimension; i++)
                for (int j = 0; j < input.dimension; j++)
                    mat.basis[i][j] = input.basis[j][i];
            return mat;
        }
        public static Matrix Identity(int dimension)
        {
            Matrix result = new Matrix(dimension);
            for (int i = 0; i < dimension; i++)
                result[i, i] = 1d;
            return result;
        }
        public static Matrix Exp(Matrix matrix)
        {
            Matrix result = Identity(matrix.dimension) + matrix * 0.00001525878d;
            for (int i = 0; i < 16; i++)
                result = Mul(result, result);
            return result;
        }

        public Matrix(int dimension)
        {
            this.dimension = dimension;
            basis = new VectorND[dimension];
            for (int i = 0; i < dimension; i++)
                basis[i] = new VectorND(new double[dimension]);
        }
        public Matrix(int dimension, params VectorND[] basis)
        {
            this.dimension = dimension;
            this.basis = new VectorND[dimension];
            for (int i = 0; i < dimension; i++)
                this.basis[i] = new VectorND(basis[i].components.ToArray());
        }
        public Matrix(int dimension, double[,] entries)
        {
            this.dimension = dimension;
            basis = new VectorND[dimension];
            List<double> doubles = new List<double>();
            for (int i = 0; i < dimension; i++)
            {
                for (int j = 0; j < dimension; j++)
                    doubles.Add(entries[i, j]);
                basis[i] = new VectorND(doubles.ToArray());
                doubles.Clear();
            }
        }
        public static Matrix EvaluateFiniteDifferenceKernel(VectorND positions)
        {
            VectorND vec = new VectorND(1);
            Matrix mat = new Matrix(positions.dimension);

            for (int i = 0; i < positions.dimension; i++)
                vec[i] = 1;

            for (int i = 0; i < mat.dimension; i++)
            {
                mat.basis[i] = vec;
                vec *= positions;
            }
            mat = Transpose(mat).inverse;

            double j = 1d;
            for (int i = 0; i < mat.dimension; i++)
            {
                mat.basis[i] *= j;
                j *= i + 1;
            }
            return mat;
        }
    }
}
