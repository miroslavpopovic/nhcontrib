﻿// Copyright 2008 - Ricardo Stuven (rstuven@gmail.com)
//
// This file is part of NHibernate.Spatial.
// NHibernate.Spatial is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// NHibernate.Spatial is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with NHibernate.Spatial; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.IO;
using GeoAPI.Geometries;
using Microsoft.SqlServer.Types;

namespace NHibernate.Spatial.Type
{
    /// <summary>
    /// 
    /// </summary>
    public class MsSql2008GeometryType : GeometryTypeBase<byte[]>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsSql2008GeometryType"/> class.
        /// </summary>
        public MsSql2008GeometryType()
            : base(NHibernateUtil.BinaryBlob)
        {
        }

        /// <summary>
        /// Sets the default SRID.
        /// </summary>
        /// <param name="geometry">The geometry.</param>
        protected override void SetDefaultSRID(IGeometry geometry)
        {
            base.SetDefaultSRID(geometry);
            if (geometry.SRID == -1)
            {
                geometry.SRID = 0;
            }
        }

        /// <summary>
        /// Converts from GeoAPI geometry type to database geometry type.
        /// </summary>
        /// <param name="value">The GeoAPI geometry value.</param>
        /// <returns></returns>
        protected override byte[] FromGeometry(object value)
        {
            IGeometry geometry = value as IGeometry;
            if (geometry == null)
            {
                return null;
            }
            else
            {
                this.SetDefaultSRID(geometry);

                try
                {
                    MsSql2008GeometryWriter writer = new MsSql2008GeometryWriter();
                    SqlGeometry sqlGeometry = writer.Write(geometry);

                    if (sqlGeometry == null)
                    {
                        return null;
                    }
                    else
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (BinaryWriter bw = new BinaryWriter(ms))
                            {
                                sqlGeometry.Write(bw);
                            }
                            return ms.ToArray();
                        }
                    }
                }
                catch (FormatException ex)
                {
                    if (ex.Message == "24117: The LineString input is not valid because it does not have enough distinct points. A LineString must have at least two distinct points." ||
                        ex.Message == "24305: The Polygon input is not valid because the ring does not have enough distinct points. Each ring of a polygon must contain at least three distinct points.")
                    {
                        // TODO: Not sure what to do in these cases...
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Converts to GeoAPI geometry type from database geometry type.
        /// </summary>
        /// <param name="value">The databse geometry value.</param>
        /// <returns></returns>
        protected override IGeometry ToGeometry(object value)
        {
            byte[] bytes = value as byte[];

            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }
            else
            {
                MsSql2008GeometryReader reader = new MsSql2008GeometryReader();
                IGeometry geometry = reader.Read(bytes);
                this.SetDefaultSRID(geometry);
                return geometry;
            }
        }

    }

}