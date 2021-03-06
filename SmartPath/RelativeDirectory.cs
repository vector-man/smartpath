﻿using System;
using System.Collections.Generic;
using System.Linq;
using HTS.SmartPath.PathFragments;

namespace HTS.SmartPath
{
	/// <summary>
	///		Represents a relative directory (one that doesn't have a root), e.g. "some directory". Has properties
	///		to express the parent directory ("..") or the empty directory (useful as a result if
	///		one attempts to get the relative path from one dir to another when they are the same)
	/// </summary>
	public struct RelativeDirectory : IEquatable<RelativeDirectory>, IFragmentProvider
	{
		/// <summary>
		///		Represents an empty directory (useful as a result for getting relative paths if they are identical).
		/// </summary>
		public static RelativeDirectory Empty = new RelativeDirectory();

		/// <summary>
		///		Represenst the parent directory in paths ("..")
		/// </summary>
		public static RelativeDirectory UpOneDirectory = new RelativeDirectory(DirectoryFragment.UpOneDirectory.Fragment);

		private readonly string m_EntireRelativePath;
		private readonly PathFragment[] m_PathFragments;

		/// <summary>
		///		The name of this directory, without any separation characters or an empty string if it is the Empty value.
		/// </summary>
		public string DirectoryName { get { return IsEmpty ? "" : m_PathFragments.Last().Fragment; } }

		/// <summary>
		///		Indicates if the path is Empty
		/// </summary>
		public bool IsEmpty { get { return string.IsNullOrEmpty(m_EntireRelativePath); }}

		/// <summary>
		///		The full path of this relative directory (i.e. with the relative directories
		///		that were included in the construction or that it was constructed from), or
		///		an empty string for the Empty value.
		/// </summary>
		public string FullPath { get { return m_EntireRelativePath ?? ""; } }

		/// <summary>
		///		Flag that indicates if this path is valid. At the moment this simply means if it is not empty,
		///		as all invalid characters etc would already be rejected by the contsturctor.
		/// </summary>
		public bool IsValid { get { return !IsEmpty; } }

		/// <summary>
		///	Enumerates the fragments of this path. This will yield nothing for the empty path, and otherwise
		/// one DirectoryFragment for every directory.
		/// </summary>
		public IEnumerable<PathFragment> PathFragments { get { return m_PathFragments ?? Enumerable.Empty<PathFragment>(); } }

		internal RelativeDirectory(RelativeDirectory parent, string relativePath)
		{
			if (string.IsNullOrWhiteSpace(relativePath))
				throw new PathInvalidException("The filename was empty");

			relativePath = relativePath.Trim();

			var entireRelativePath = PathUtilities.EnsureEndsWithBackslash(parent.m_EntireRelativePath + relativePath);

			var match = WindowsPathDetails.RelativePathRegex.Match(entireRelativePath);
			if (!match.Success || match.Groups["root"].Success)
				throw new PathInvalidException("The give path was not just a relative folder: " + relativePath);

			m_EntireRelativePath = entireRelativePath;

			m_PathFragments = PathUtilities.GetPathFragments(match, true).ToArray();
		}

		internal RelativeDirectory(string relativePath)
		{
			if (string.IsNullOrWhiteSpace(relativePath))
				throw new PathInvalidException("The filename was empty");

			relativePath = relativePath.Trim();

			var match = WindowsPathDetails.RelativePathRegex.Match(relativePath);
			if (!match.Success || match.Groups["root"].Success)
				throw new PathInvalidException("The give path was not just a relative folder: " + relativePath);

			m_PathFragments = PathUtilities.GetPathFragments(match, true).ToArray();

			if (m_PathFragments.Length == 0)
			{
				m_EntireRelativePath = "";
			}
			else
			{
				m_EntireRelativePath = PathUtilities.EnsureEndsWithBackslash(relativePath);
			}
				
		}

		/// <summary>
		///		Creates a new RelativeDirectory below this one.
		/// </summary>
		/// <param name="directoryName">Name of the new directory.</param>
		/// <returns></returns>
		public RelativeDirectory CreateDirectoryPath(string directoryName)
		{
			return new RelativeDirectory(this, directoryName);
		}

		/// <summary>
		///		Creates a new RelativeFilename below this directory.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public RelativeFilename CreateFilename(string filename)
		{
			return new RelativeFilename(this, filename);
		}

		/// <summary>
		///		Creates a RelativeDirectory from a string. 
		/// </summary>
		/// <param name="relativePath"></param>
		/// <param name="throwExceptionForInvalidPaths"></param>
		/// <returns>The new RelativeDirectory or null if the string was invalid.</returns>
		/// <exception cref="PathInvalidException">Thrown if the parameter throwExceptionForInvalidPaths is true and the path is invalid.</exception>
		public static RelativeDirectory FromPathString(string relativePath, bool throwExceptionForInvalidPaths = false)
		{
			if (!throwExceptionForInvalidPaths && string.IsNullOrWhiteSpace(relativePath))
				return Empty;
			try
			{
				return new RelativeDirectory(relativePath);
			}
			catch (PathInvalidException)
			{
				if (throwExceptionForInvalidPaths)
					throw;

				return Empty;
			}
		}

		public static RelativeDirectory FromPathFragments(IEnumerable<PathFragment> fragments, bool throwExceptionForInvalidPaths = false)
		{
			RelativeDirectory returnVal = Empty;

			if (fragments.Any())
			{
				if (fragments.First() is RootFragment)
				{
					if (throwExceptionForInvalidPaths)
						throw new PathInvalidException("RelativeDirectory must not start with a root");
				}
				else if (fragments.Last() is FileFragment)
				{
					if (throwExceptionForInvalidPaths)
						throw new PathInvalidException("RelativeDirectory must not end with a file");
				}
				else if (!fragments.All(fragment => fragment is DirectoryFragment))
				{
					if (throwExceptionForInvalidPaths)
						throw new PathInvalidException("RelativeDirectory must only contain directories");
				}
				else
					returnVal = new RelativeDirectory(string.Join("", fragments.Select(fragment => fragment.ConcatenableFragment)));
			}
			return returnVal;
		}

		/// <summary>
		/// Indicates whether this instance and a specified object are equal.
		/// </summary>
		/// <returns>
		/// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
		/// </returns>
		/// <param name="obj">Another object to compare to. </param><filterpriority>2</filterpriority>
		public override bool Equals(object obj)
		{
			if (!(obj is RelativeDirectory))
				return false;

			return Equals((RelativeDirectory) obj);
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this instance.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override int GetHashCode()
		{
			return FullPath.ToLower().GetHashCode();
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public bool Equals(RelativeDirectory other)
		{
			if (IsEmpty ^ other.IsEmpty) // it either is empty but not the other
				return false;

			if (IsEmpty && other.IsEmpty)
				return true;

			return StringComparer.InvariantCultureIgnoreCase.Equals(m_EntireRelativePath, other.m_EntireRelativePath);
		}

		/// <summary>
		/// Returns the fully qualified type name of this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> containing a fully qualified type name.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			return FullPath;
		}

		public static bool operator ==(RelativeDirectory one, RelativeDirectory two)
		{
			return one.Equals(two);
		}

		public static bool operator !=(RelativeDirectory one, RelativeDirectory two)
		{
			return !(one == two);
		}

		[Obsolete("Use IsValid instead of comparing this value type to null!")]
		public static bool operator ==(RelativeDirectory one, RelativeDirectory? two)
		{
			return false;
		}

		[Obsolete("Use IsEmpty instead of comparing this value type to null!")]
		public static bool operator !=(RelativeDirectory one, RelativeDirectory? two)
		{
			return true;
		}

		[Obsolete("Use IsValid instead of comparing this value type to null!")]
		public static bool operator ==(RelativeDirectory? one, RelativeDirectory two)
		{
			return false;
		}

		[Obsolete("Use IsEmpty instead of comparing this value type to null!")]
		public static bool operator !=(RelativeDirectory? one, RelativeDirectory two)
		{
			return true;
		}
	}
}
