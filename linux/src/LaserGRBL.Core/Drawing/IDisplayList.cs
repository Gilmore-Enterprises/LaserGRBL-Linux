namespace LaserGRBL
{
	/// <summary>
	/// Minimal display-invalidation seam so GrblCommand can hold a reference to a render
	/// list and signal that it changed, without depending on SharpGL/WinForms/Avalonia.
	/// The App layer (GrblPanel3D port) implements this with Silk.NET/OpenTK display lists.
	/// </summary>
	public interface IDisplayList
	{
		void Invalidate();
	}
}
