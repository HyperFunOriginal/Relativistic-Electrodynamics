# Relativistic-Electrodynamics

## Contents
1. Equations
2. Numerical Implementation
3. Editor Variables

# Equations
Large thanks to _Luis Lehner, Vasileios Paschalidis, and Frans Pretorius_ for the following resource: [https://lavinia.as.arizona.edu/~vpaschal/maxwell_constraint_damping.pdf]
<br /> <br />

$$\partial_t E^i = {\varepsilon^{i j}}_k \partial_j B^k - \partial^i \psi - \mathcal{J}^i$$

$$\partial_t B^i = -{\varepsilon^{i j}}_k \partial_j E^k - \partial^i \phi$$

$$\partial_t \phi = -\partial_i B^i - \sigma \phi$$

$$\partial_t \psi = \mathcal{J}^t - \partial_i E^i - \sigma \psi$$

# Numerical Implementation
The fields are stored as large arrays of size $N^3$, and relevant derivatives are computed using $6^{th}$ order finite difference, with adaptive coefficients at the boundaries.
Derivatives are coordinate-transformed such that voxels far from the centre of the domain represent larger physical distances, with the farthest voxel stretching out to infinity.
Derivatives are also cached to allow for sommerfeld radiation evolution at the boundaries.

The fields are evolved with the Semi-implicit Euler method, with appropriate $6^{th}$ order Kreiss-Oliger Dissipation. [https://einsteintoolkit.org/thornguide/CactusNumerical/Dissipation/documentation.html].

For rendering, the field and derivative arrays are read with the reverse coordinate transformation, and interpolated to yield a smoother picture.

# Editor Variables
`Vector3Int resolution`: Size of the domain in voxels along the x, y and z axes respectively. <br />
`float lengthScale`: Physical size of 1 voxel (at the center of the domain). Units are in `light-seconds`. <br />
`float timestep`: Timestep size for numerical evolution. Units are in `seconds`. <br /> <br />
`float slice`: Z-axis position of the XY plane rendered to the screen. The center of the domain is `resolution.z / 2` <br />
`float zoom`: Rendered scale of the domain. Magnification is $10^{\textup{zoom}}$. <br />
`float vectorScale`: Rendered scale of vector field. Vector length scale is multiplied by $10^{\textup{scale}}$. <br />
`int recordInterval`: Number of simulation frames per frame saved to disk. <br />
`int recordCutoff`: Number of frames to save to disk. <br /> <br />
`ComputeShader shader`: Compute shader used for computation and rendering. <br />
`RenderTexture screen`: Render texture that `shader` renders to. <br /> <br /> <br />

To modify the field rendered to screen, modify the compute buffers used in computing the variable `trueF` in the compute kernel `PrintImage`.
