using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
//* Copyright 2004 University Corporation for Atmospheric Research/Unidata
//* 
//* Portions of this software were developed by the Unidata Program at the 
//* University Corporation for Atmospheric Research.
//* 
//* Access and use of this software shall impose the following obligations
//* and understandings on the user. The user is granted the right, without
//* any fee or cost, to use, copy, modify, alter, enhance and distribute
//* this software, and any derivative works thereof, and its supporting
//* documentation for any purpose whatsoever, provided that this entire
//* notice appears in all copies of the software, derivative works and
//* supporting documentation.  Further, UCAR requests that the user credit
//* UCAR/Unidata in any publications that result from the use of this
//* software or in any product that includes this software. The names UCAR
//* and/or Unidata, however, may not be used in any advertising or publicity
//* to endorse or promote any products or commercial entity unless specific
//* written permission is obtained from UCAR/Unidata. The user also
//* understands that UCAR/Unidata is not obligated to provide the user with
//* any support, consulting, training or assistance of any kind with regard
//* to the use, operation and performance of this software nor to provide
//* the user with any updates, revisions, new versions or "bug fixes."
//* 
//* THIS SOFTWARE IS PROVIDED BY UCAR/UNIDATA "AS IS" AND ANY EXPRESS OR
//* IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//* DISCLAIMED. IN NO EVENT SHALL UCAR/UNIDATA BE LIABLE FOR ANY SPECIAL,
//* INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING
//* FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT,
//* NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION
//* WITH THE ACCESS, USE OR PERFORMANCE OF THIS SOFTWARE.
//
//This is a wrapper class for the netCDF dll.
//
//Get the netCDF dll from ftp://ftp.unidata.ucar.edu/pub/netcdf/contrib/win32
//Put it somewhere in your path, or else in the bin subdirectory of your
//VB project.
//
//Then include this class file in your project. Use the netcdf functions 
//like this:
//res = NetCDF.nc_create(name, NetCDF.cmode.NC_CLOBBER, ncid)
//If (res <> 0) Then GoTo err
//
//NetCDF was ported to dll by John Caron (as far as I know).
//This VB.NET wrapper created by Ed Hartnett, 3/10/4
//
//Some notes:
//   Although the dll can be tested (and has passed for release 
//3.5.0 and 3.5.1 at least), the VB wrapper class has not been
//extensively tested. Use at your own risk. Writing test code to
//test the netCDF interface is a non-trivial task, and one I haven't
//undertaken. The tests run verify common use of netCDF, for example
//creation of dims, vars, and atts of various types, and ensuring that
//they can be written and read back. But I don't check type conversion,
//or boundery conditions. These are all tested in the dll, but not the
//VB wrapper.
//
//This class consists mearly of some defined enums, consts and declares, 
//all inside a class called NetCDF.
//
//Passing strings: when passing in a string to a function, use a string,
//when passing in a pointer to a string so that the function can fill it 
//(for example when requesting an attribute name, use a 
//System.Text.StringBuilder.
//
//Since VB doesn't have an unsigned byte, I've left those functions 
//out of the wrapper class. If you need to read unsigned bytes, read them as 
//shorts, and netcdf will automatically convert them for you.
//
//The C interface allows you to read and write to a 
//
using System.Runtime.InteropServices;
using System.Text;
namespace ASA.NetCDF4 {
    internal class NetCDF
    {

        // The netcdf external data types
        public enum nc_type
        {
            NC_NAT      = 0,
            NC_BYTE     = 1,
            NC_CHAR     = 2,
            NC_SHORT    = 3,
            NC_INT      = 4,
            NC_LONG     = 4,
            NC_FLOAT    = 5,
            NC_DOUBLE   = 6,
            NC_UBYTE    = 7,
            NC_USHORT   = 8,
            NC_UINT     = 9,
            NC_INT64    = 10,
            NC_UINT64   = 11,
            NC_STRING   = 12,
            NC_VLEN     = 13,
            NC_OPAQUE   = 14,
            NC_ENUM     = 15,
            NC_COMPOUND = 16

        }

        public enum cmode
        {
            NC_NOWRITE = 0,
            NC_WRITE = 0x1,
            ///* read & write */
            NC_CLOBBER = 0,
            NC_NOCLOBBER = 0x4,
            ///* Don't destroy existing file on create */
            NC_FILL = 0,
            ///* argument to ncsetfill to clear NC_NOFILL */
            NC_NOFILL = 0x100,
            ///* Don't fill data section an records */
            NC_LOCK = 0x400,
            ///* Use locking if available */
            NC_SHARE = 0x800
            ///* Share updates, limit cacheing */
        }

        ///*
        // *     Default fill values, used unless _FillValue attribute is set.
        // * These values are stuffed into newly allocated space as appropriate.
        // * The hope is that one might use these to notice that a particular datum
        // * has not been set.
        // */
        public const byte NC_FILL_BYTE = 255;
        public const byte NC_FILL_CHAR = 0;
        public const Int16 NC_FILL_SHORT = -32767;
        public const Int32 NC_FILL_INT = -2147483647;
            ///* near 15 * 2^119 */
        public const float NC_FILL_FLOAT = 9.96921E+36F;

        public const double NC_FILL_DOUBLE = 9.96920996838687E+36;
        //* 'size' argument to ncdimdef for an unlimited dimension

        public const Int32 NC_UNLIMITED = 0;
        //* attribute id to put/get a global attribute

        public const Int32 NC_GLOBAL = -1;
        //* These maximums are enforced by the interface, to facilitate writing
        //* applications and utilities.  However, nothing is statically allocated to
        //* these sizes internally.
        public enum netCDF_limits
        {
            NC_MAX_DIMS = 10,
            ///* max dimensions per file */
            NC_MAX_ATTRS = 2000,
            ///* max global or per variable attributes */
            NC_MAX_VARS = 2000,
            ///* max variables per file */
            NC_MAX_NAME = 256,
            ///* max length of a name */
            NC_MAX_VAR_DIMS = 10
            ///* max per variable dimensions */
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct vlen_t {
            public Int32 len;  // size_t
            public IntPtr data; // void *
        }
        // const char *nc_inq_libvers(void);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern IntPtr nc_inq_libvers();

        public static string libvers() {
            IntPtr p = nc_inq_libvers();
            return Marshal.PtrToStringAnsi(p);
        }
        
        // const char *nc_strerror(int ncerr1);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern string nc_strerror(Int32 ncerr1);
        
        // int nc_create(const char *path, int cmode, int *ncidp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_create(string path, Int32 cmode, ref Int32 ncidp);
        
        // int nc_open(const char *path, int mode, int *ncidp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_open(string path, Int32 cmode, ref Int32 ncidp);
        
        // int nc_set_fill(int ncid, int fillmode, int *old_modep);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_set_fill(Int32 ncid, Int32 fillmode, ref Int32 old_modep);
        
        // int nc_redef(int ncid);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_redef(Int32 ncid);
        
        // int nc_enddef(int ncid);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_enddef(Int32 ncid);
        
        // int nc_sync(int ncid);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_sync(Int32 ncid);
        
        // int nc_abort(int ncid);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_abort(Int32 ncid);
        
        // int nc_close(int ncid);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_close(Int32 ncid);
        
        // INQ FUNCTIONS
        //
        // int nc_inq(int ncid, int *ndimsp, int *nvarsp, int *nattsp, int *unlimdimidp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq(Int32 ncid, ref Int32 ndimsp, ref Int32 nvarsp, ref Int32 nattsp, ref Int32 unlimdimidp);
        
        // int  nc_inq_ndims(int ncid, int *ndimsp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_ndims(Int32 ncid, ref Int32 ndimsp);
        
        // int  nc_inq_nvars(int ncid, int *nvarsp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_nvars(Int32 ncid, ref Int32 nvarsp);
        
        // int  nc_inq_natts(int ncid, int *nattsp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_natts(Int32 ncid, ref Int32 nattsp);
        
        // int  nc_inq_unlimdim(int ncid, int *unlimdimidp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_unlimdim(Int32 ncid, ref Int32 unlimdimidp);
        
        // int nc_def_dim(int ncid, const char *name, size_t len, int *idp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_dim(Int32 ncid, string name, Int32 len, ref Int32 idp);
        
        // int nc_inq_dimid(int ncid, const char *name, int *idp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_dimid(Int32 ncid, string name, ref Int32 idp);
        
        // int nc_inq_dim(int ncid, int dimid, char *name, size_t *lenp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_dim(Int32 ncid, Int32 dimid, StringBuilder name, ref Int32 lenp);
        
        // int  nc_inq_dimname(int ncid, int dimid, char *name);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_dimname(Int32 ncid, Int32 dimid, StringBuilder name);
        
        // int  nc_inq_dimlen(int ncid, int dimid, size_t *lenp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_dimlen(Int32 ncid, Int32 dimid, ref Int32 lenp);
        
        // int nc_rename_dim(int ncid, int dimid, const char *name);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_rename_dim(Int32 ncid, Int32 dimid, string name);
        
        // int nc_inq_att(int ncid, int varid, const char *name, nc_type *xtypep, size_t *lenp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_att(Int32 ncid, Int32 varid, string name, ref Int32 xtypep, ref Int32 lenp);
        
        // int  nc_inq_attid(int ncid, int varid, const char *name, int *idp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_attid(Int32 ncid, Int32 varid, string name, ref Int32 xtypep, ref Int32 lenp);
        
        // int  nc_inq_atttype(int ncid, int varid, const char *name, nc_type *xtypep);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_atttype(Int32 ncid, Int32 varid, string name, ref Int32 xtypep);
        
        // int  nc_inq_attlen(int ncid, int varid, const char *name, size_t *lenp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_attlen(Int32 ncid, Int32 varid, string name, ref Int32 lenp);
        
        // int nc_inq_attname(int ncid, int varid, int attnum, char *name);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_attname(Int32 ncid, Int32 varid, Int32 attnum, 
            [In(), Out()] byte[] name);

        // ATTRIBUTE READING AND WRITING

        // int nc_copy_att(int ncid_in, int varid_in, const char *name, int ncid_out, int varid_out);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_copy_att(Int32 ncid_in, Int32 varid_in, string name, Int32 ncid_out, Int32 varid_out);
        
        // int nc_rename_att(int ncid, int varid, const char *name, const char *newname);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_rename_att(Int32 ncid, Int32 varid, string name, ref string newname);
        
        // int nc_del_att(int ncid, int varid, const char *name);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_del_att(Int32 ncid, Int32 varid, string name);

        //int nc_put_att(int ncid, int varid, const char *name, nc_type xtype, size_t len, const void *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len, ref vlen_t op);
        
        //int nc_get_att(int ncid, int varid, const char *name, void *ip)
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att(Int32 ncid, Int32 varid, string name, ref vlen_t op);
        
        
        // int nc_put_att_text(int ncid, int varid, const char *name,
        //    size_t len, const char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_text(Int32 ncid, Int32 varid, string name, Int32 len, string op);
        
        // int nc_get_att_text(int ncid, int varid, const char *name, char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_text(Int32 ncid, Int32 varid, string name, 
            [In(), Out()]    byte[] ip);
        
        // int nc_put_att_uchar(int ncid, int varid, const char *name, nc_type xtype,
        //    size_t len, const unsigned char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_uchar(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,
            [In()] byte[] op);
        
        // int nc_get_att_uchar(int ncid, int varid, const char *name, unsigned char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_uchar(Int32 ncid, Int32 varid, string name,     
            [In(), Out()]    byte[] ip);
        
        // int nc_put_att_schar(int ncid, int varid, const char *name, nc_type xtype,
        //    size_t len, const signed char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_schar(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,     
            [In()]    sbyte[] op);
        
        // int nc_get_att_schar(int ncid, int varid, const char *name, signed char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_schar(Int32 ncid, Int32 varid, string name,     
            [In(), Out()]    sbyte[] ip);
        
        // int nc_put_att_short(int ncid, int varid, const char *name, nc_type xtype,
        //    size_t len, const short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_short(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,     
            [In()]    Int16[] op);
        
        // int nc_get_att_short(int ncid, int varid, const char *name, short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_short(Int32 ncid, Int32 varid, string name,     
            [In(), Out()]    Int16[] ip);
        
        // int nc_put_att_ushort(int ncid, int varid, const char *name, nc_type xtype,
        //    size_t len, const short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_ushort(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,     
            [In()]    UInt16[] op);
        
        // int nc_get_att_ushort(int ncid, int varid, const char *name, short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_ushort(Int32 ncid, Int32 varid, string name,     
            [In(), Out()]    UInt16[] ip);
        
        // int nc_put_att_int(int ncid, int varid, const char *name, nc_type xtype,
        //    size_t len, const int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_int(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,     
            [In()]    Int32[] op);
        
        // int nc_get_att_int(int ncid, int varid, const char *name, int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_int(Int32 ncid, Int32 varid, string name,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_att_uint(int ncid, int varid, const char *name, nc_type xtype,
        //    size_t len, const int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_uint(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,     
            [In()]    UInt32[] op);
        
        // int nc_get_att_uint(int ncid, int varid, const char *name, int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_uint(Int32 ncid, Int32 varid, string name,     
            [In(), Out()]    UInt32[] ip);
        
        // int nc_put_att_long(int ncid, int varid, const char *name, nc_type xtype,
        //    size_t len, const long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_long(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,     
            [In()]    Int32[] op);
        
        // int nc_get_att_long(int ncid, int varid, const char *name, long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_long(Int32 ncid, Int32 varid, string name,     
            [In(), Out()]    Int32[] ip);

        //int nc_put_att_longlong(int ncid, int varid, const char *name, nc_type xtype, size_t len, const long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_longlong(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,
            [In()]  Int64[] op);

        //int nc_get_att_longlong(int ncid, int varid, const char *name, long long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_longlong(Int32 ncid, Int32 varid, string name, 
            [In(), Out()] Int64[] ip);

        //int nc_put_att_ulonglong(int ncid, int varid, const char *name, nc_type xtype, size_t len, const unsigned long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_ulonglong(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,
            [In()] UInt64[] op);

        //int nc_get_att_ulonglong(int ncid, int varid, const char *name, unsigned long long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_ulonglong(Int32 ncid, Int32 varid, string name,
            [In(), Out()] UInt64[] ip);
        
        // int nc_put_att_float(int ncid, int varid, const char *name, nc_type xtype,
        //    size_t len, const float *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_float(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,     
            [In()]    float[] op);
        
        // int nc_get_att_float(int ncid, int varid, const char *name, float *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_float(Int32 ncid, Int32 varid, string name,     
            [In(), Out()]    float[] ip);
        
        // int nc_put_att_double(int ncid, int varid, const char *name, nc_type xtype,
        //    size_t len, const double *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_att_double(Int32 ncid, Int32 varid, string name, Int32 xtype, Int32 len,     
            [In()]    double[] op);
        
        // int nc_get_att_double(int ncid, int varid, const char *name, double *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_att_double(Int32 ncid, Int32 varid, string name,     
            [In(), Out()]    double[] ip);
        
        // VARIABLE CREATION AND INQ

        // int nc_def_var(int ncid, const char *name,
        //     nc_type xtype, int ndims, const int *dimidsp, int *varidp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_var(Int32 ncid, string name, Int32 xtype, Int32 ndims,     
            [In()]    int[] dimids, ref Int32 varid);
        
        // int nc_inq_var(int ncid, int varid, char *name, nc_type *xtypep, int *ndimsp, int *dimidsp, int *nattsp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_var(Int32 ncid, Int32 varid, StringBuilder name, ref Int32 xtypep, ref Int32 ndimsp,     
            [Out()]    int[] dimidsp, ref Int32 nattsp);
        // int nc_inq_varid(int ncid, const char *name, int *varidp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_varid(Int32 ncid, string name, ref Int32 varid);
        
        // int  nc_inq_varname(int ncid, int varid, char *name);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_varname(Int32 ncid, Int32 varid, StringBuilder name);
        
        // int  nc_inq_vartype(int ncid, int varid, nc_type *xtypep);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_vartype(Int32 ncid, Int32 varid, ref Int32 xtypep);
        
        // int  nc_inq_varndims(int ncid, int varid, int *ndimsp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_varndims(Int32 ncid, Int32 varid, ref Int32 ndimsp);
        
        // int  nc_inq_vardimid(int ncid, int varid, int *dimidsp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_vardimid(Int32 ncid, Int32 varid,    [Out()] Int32[] dimidsp);
        
        // int  nc_inq_varnatts(int ncid, int varid, int *nattsp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_varnatts(Int32 ncid, Int32 varid, ref Int32 nattsp);
        
        // int nc_rename_var(int ncid, int varid, const char *name);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_rename_var(Int32 ncid, Int32 varid, string name);
        //READING AND WRITING ONE VALUE AT A TIME
        //
        // int nc_put_var1_text(int ncid, int varid, const size_t *indexp, const char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp, 
            [In(), Out()]    byte[] op);
        
        // int nc_get_var1_text(int ncid, int varid, const size_t *indexp, char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp, 
            [In(), Out()]    byte[] ip);
        
        // int nc_put_var1_text(int ncid, int varid, const size_t *indexp, const char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp, 
            [In(), Out()]    sbyte[] op);
        
        // int nc_get_var1_text(int ncid, int varid, const size_t *indexp, char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp, 
            [In(), Out()]    sbyte[] ip);
        
        // int nc_put_var1_uchar(int ncid, int varid, const size_t *indexp,
        //    const unsigned char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_uchar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    byte[] op);
        
        // int nc_get_var1_uchar(int ncid, int varid, const size_t *indexp,
        //    unsigned char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_uchar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    byte[] ip);
        
        // int nc_put_var1_schar(int ncid, int varid, const size_t *indexp,
        //    const signed char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_schar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    sbyte[] op);
        
        // int nc_get_var1_schar(int ncid, int varid, const size_t *indexp,
        //    signed char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_schar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    sbyte[] ip);
        
        // int nc_put_var1_short(int ncid, int varid, const size_t *indexp,
        //    const short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_short(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    Int16[] op);
        
        // int nc_get_var1_short(int ncid, int varid, const size_t *indexp,
        //    short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_short(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    Int16[] ip);
        
        // int nc_put_var1_ushort(int ncid, int varid, const size_t *indexp,
        //    const unsigned short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_ushort(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    UInt16[] op);
        
        // int nc_get_var1_ushort(int ncid, int varid, const size_t *indexp,
        //    unsigned short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_ushort(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    UInt16[] ip);
        
        // int nc_put_var1_int(int ncid, int varid, const size_t *indexp, const int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_int(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_var1_int(int ncid, int varid, const size_t *indexp, int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_int(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_var1_uint(int ncid, int varid, const size_t *indexp, const unsigned int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_uint(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    UInt32[] op);
        
        // int nc_get_var1_uint(int ncid, int varid, const size_t *indexp, unsigned int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_uint(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    UInt32[] ip);
        
        // int nc_put_var1_long(int ncid, int varid, const size_t *indexp, const long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_long(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_var1_long(int ncid, int varid, const size_t *indexp, long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_long(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    Int32[] ip);

        //int nc_put_var1_longlong(int ncid, int varid, const size_t *indexp, const long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_longlong(Int32 ncid, Int32 varid, 
            [In()]    Int32[] indexp,
            [In(), Out()]    Int64[] ip);
        
        //int nc_get_var1_longlong(int ncid, int varid, const size_t *indexp, const long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_longlong(Int32 ncid, Int32 varid, 
            [In()]    Int32[] indexp,
            [In(), Out()]    Int64[] ip);
        
        //int nc_put_var1_ulonglong(int ncid, int varid, const size_t *indexp, const unsigned long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_ulonglong(Int32 ncid, Int32 varid, 
            [In()]    Int32[] indexp,
            [In(), Out()]    UInt64[] ip);
        
        //int nc_get_var1_ulonglong(int ncid, int varid, const size_t *indexp, const unsigned long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_ulonglong(Int32 ncid, Int32 varid, 
            [In()]    Int32[] indexp,
            [In(), Out()]    UInt64[] ip);
        
        // int nc_put_var1_float(int ncid, int varid, const size_t *indexp, const float *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_float(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    float[] op);
        
        // int nc_get_var1_float(int ncid, int varid, const size_t *indexp, float *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_float(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    float[] ip);
        
        // int nc_put_var1_double(int ncid, int varid, const size_t *indexp, const double *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1_double(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    double[] op);
        
        // int nc_get_var1_double(int ncid, int varid, const size_t *indexp, double *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1_double(Int32 ncid, Int32 varid,     
            [In()]    Int32[] indexp,     
            [In(), Out()]    double[] ip);
        
        //READING AND WRITING SUBSETS OF ARRAYS, WITH START AND COUNT ARRAYS

        // int nc_put_vara_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp, 
            [In(), Out()]    byte[]  op);
        
        // int nc_get_vara_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp, 
            [In(), Out()]    byte[] op);
        
        // int nc_put_vara_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp, 
            [In(), Out()]    sbyte[]  op);
        
        // int nc_get_vara_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp, 
            [In(), Out()]    sbyte[] op);
        
        // int nc_put_vara_uchar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const unsigned char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_uchar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    byte[] op);
        
        // int nc_get_vara_uchar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, unsigned char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_uchar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    byte[] ip);
        
        // int nc_put_vara_schar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const signed char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_schar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    sbyte[] op);
        
        // int nc_get_vara_schar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, signed char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_schar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    sbyte[] ip);
        
        // int nc_put_vara_short(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_short(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    Int16[] op);
        
        // int nc_get_vara_short(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_short(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    Int16[] ip);
        
        // int nc_put_vara_ushort(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const unsigned short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_ushort(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    UInt16[] op);
        
        // int nc_get_vara_ushort(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, unsigned short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_ushort(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    UInt16[] ip);
        
        // int nc_put_vara_int(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_int(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_vara_int(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_int(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_vara_uint(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const unsigned int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_uint(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    UInt32[] op);
        
        // int nc_get_vara_uint(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, unsigned int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_uint(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    UInt32[] ip);
        
        // int nc_put_vara_long(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_long(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_vara_long(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_long(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_vara_longlong(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_longlong(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    Int64[] op);
        
        // int nc_get_vara_longlong(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, long long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_longlong(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    Int64[] ip);
        
        // int nc_put_vara_ulonglong(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const unsigned long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_ulonglong(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    UInt64[] op);
        
        // int nc_get_vara_ulonglong(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, unsigned long long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_ulonglong(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    UInt64[] ip);
        
        // int nc_put_vara_float(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const float *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_float(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    float[] op);
        
        // int nc_get_vara_float(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, float *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_float(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    float[] ip);
        
        // int nc_put_vara_double(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const double *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vara_double(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    double[] op);
        
        // int nc_get_vara_double(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, double *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vara_double(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In(), Out()]    double[] ip);

        //READING AND WRITING SUBSETS OF ARRAYS WITH START, COUNT, and STRIDE ARRAYS

        // int nc_put_vars_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep, 
            [In(), Out()]    byte[] op);
        
        // int nc_get_vars_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep, 
            [In(), Out()]    byte[] op);
        
        // int nc_put_vars_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep, 
            [In(), Out()]    sbyte[] op);
        
        // int nc_get_vars_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep, 
            [In(), Out()]    sbyte[] op);
        
        // int nc_put_vars_uchar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const unsigned char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_uchar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    byte[] op);
        
        // int nc_get_vars_uchar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    unsigned char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_uchar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    byte[] ip);
        
        // int nc_put_vars_schar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const signed char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_schar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    sbyte[] op);
        
        // int nc_get_vars_schar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    signed char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_schar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    sbyte[] ip);
        
        // int nc_put_vars_short(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_short(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int16[] op);
        
        // int nc_get_vars_short(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_short(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int16[] ip);
        
        // int nc_put_vars_ushort(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const unsigned short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_ushort(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    UInt16[] op);
        
        // int nc_get_vars_ushort(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    unsigned short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_ushort(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    UInt16[] ip);
        
        // int nc_put_vars_int(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_int(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_vars_int(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_int(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_vars_uint(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const unsigned int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_uint(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    UInt32[] op);
        
        // int nc_get_vars_uint(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    unsigned int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_uint(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    UInt32[] ip);
        
        // int nc_put_vars_long(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_long(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_vars_long(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_long(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_vars_longlong(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_longlong(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int64[] op);
        
        // int nc_get_vars_longlong(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    long long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_longlong(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int64[] ip);
        
        // int nc_put_vars_ulonglong(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const unsigned long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_ulonglong(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    UInt64[] op);
        
        // int nc_get_vars_ulonglong(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    unsigned long long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_ulonglong(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    UInt64[] ip);
        
        // int nc_put_vars_float(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const float *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_float(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    float[] op);
        
        // int nc_get_vars_float(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    float *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_float(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    float[] ip);
        
        // int nc_put_vars_double(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const double *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_vars_double(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    double[] op);
        
        // int nc_get_vars_double(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    double *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_vars_double(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    double[] ip);

        //READING AND WRITING MAPPED ARRAYS

        // int nc_put_varm_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    const char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_varm_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp, 
            [In(), Out()]    byte[]  op);
        
        // int nc_get_varm_text(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_varm_text(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp, 
            [In(), Out()]    byte[]  op);
        
        // int nc_put_varm_uchar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    const unsigned char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_varm_uchar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    byte[] op);
        
        // int nc_get_varm_uchar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    unsigned char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_varm_uchar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    byte[] ip);
        
        // int nc_put_varm_schar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    const signed char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_varm_schar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    sbyte[] op);
        
        // int nc_get_varm_schar(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    signed char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_varm_schar(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    sbyte[] ip);
        
        // int nc_put_varm_short(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    const short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_varm_short(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    Int16[] op);
        
        // int nc_get_varm_short(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_varm_short(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    Int16[] ip);
        
        // int nc_put_varm_int(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    const int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_varm_int(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_varm_int(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_varm_int(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_varm_long(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    const long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_varm_long(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_varm_long(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_varm_long(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_varm_float(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    const float *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_varm_float(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    float[] op);
        
        // int nc_get_varm_float(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    float *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_varm_float(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    float[] ip);
        
        // int nc_put_varm_double(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t *imapp, 
        //    const double *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_varm_double(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    double[] op);
        
        // int nc_get_varm_double(int ncid, int varid,
        //    const size_t *startp, const size_t *countp, const ptrdiff_t *stridep,
        //    const ptrdiff_t * imap, 
        //    double *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_varm_double(Int32 ncid, Int32 varid,     
            [In()]    Int32[] startp,     
            [In()]    Int32[] countp,     
            [In()]    Int32[] stridep,     
            [In(), Out()]    Int32[] imapp,     
            [In(), Out()]    double[] ip);
        
        //READING AND WRITING VARS ALL AT ONCE
        //
        // int nc_put_var_text(int ncid, int varid, const char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_text(Int32 ncid, Int32 varid, 
            [In(), Out()]    byte[] op);
        
        // int nc_get_var_text(int ncid, int varid, char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_text(Int32 ncid, Int32 varid, 
            [In(), Out()]    byte[] ip);
        
        // int nc_put_var_text(int ncid, int varid, char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_text(Int32 ncid, Int32 varid, 
            [In(), Out()]    sbyte[] ip);
        
        // int nc_get_var_text(int ncid, int varid, char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_text(Int32 ncid, Int32 varid, 
            [In(), Out()]    sbyte[] ip);
        
        // int nc_put_var_uchar(int ncid, int varid, const unsigned char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_uchar(Int32 ncid, Int32 varid,     
            [In(), Out()]    byte[] op);
        
        // int nc_get_var_uchar(int ncid, int varid, unsigned char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_uchar(Int32 ncid, Int32 varid,     
            [In(), Out()]    byte[] ip);
        
        // int nc_put_var_schar(int ncid, int varid, const signed char *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_schar(Int32 ncid, Int32 varid,     
            [In(), Out()]    sbyte[] op);
        
        // int nc_get_var_schar(int ncid, int varid, signed char *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_schar(Int32 ncid, Int32 varid,     
            [In(), Out()]    sbyte[] ip);
        
        // int nc_put_var_short(int ncid, int varid, const short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_short(Int32 ncid, Int32 varid,     
            [In(), Out()]    Int16[] op);
        
        // int nc_get_var_short(int ncid, int varid, short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_short(Int32 ncid, Int32 varid,     
            [In(), Out()]    Int16[] ip);
        
        // int nc_put_var_ushort(int ncid, int varid, const unsigned short *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_ushort(Int32 ncid, Int32 varid,     
            [In(), Out()]    UInt16[] op);
        
        // int nc_get_var_ushort(int ncid, int varid, unsigned short *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_ushort(Int32 ncid, Int32 varid,     
            [In(), Out()]    UInt16[] ip);
        
        // int nc_put_var_int(int ncid, int varid, const int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_int(Int32 ncid, Int32 varid,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_var_int(int ncid, int varid, int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_int(Int32 ncid, Int32 varid,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_var_uint(int ncid, int varid, const unsgined int *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_uint(Int32 ncid, Int32 varid,     
            [In(), Out()]    UInt32[] op);
        
        // int nc_get_var_uint(int ncid, int varid, unsigned int *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_uint(Int32 ncid, Int32 varid,     
            [In(), Out()]    UInt32[] ip);
        
        // int nc_put_var_long(int ncid, int varid, const long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_long(Int32 ncid, Int32 varid,     
            [In(), Out()]    Int32[] op);
        
        // int nc_get_var_long(int ncid, int varid, long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_long(Int32 ncid, Int32 varid,     
            [In(), Out()]    Int32[] ip);
        
        // int nc_put_var_longlong(int ncid, int varid, const long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_longlong(Int32 ncid, Int32 varid,     
            [In(), Out()]    Int64[] op);
        
        // int nc_get_var_longlong(int ncid, int varid, long long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_longlong(Int32 ncid, Int32 varid,     
            [In(), Out()]    Int64[] ip);
        
        // int nc_put_var_ulonglong(int ncid, int varid, const unsigned long long *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_ulonglong(Int32 ncid, Int32 varid,     
            [In(), Out()]    UInt64[] op);
        
        // int nc_get_var_ulonglong(int ncid, int varid, unsigned long long *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_ulonglong(Int32 ncid, Int32 varid,     
            [In(), Out()]    UInt64[] ip);
        
        // int nc_put_var_float(int ncid, int varid, const float *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_float(Int32 ncid, Int32 varid,     
            [In(), Out()]    float[] op);
        
        // int nc_get_var_float(int ncid, int varid, float *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_float(Int32 ncid, Int32 varid,     
            [In(), Out()]    float[] ip);
        
        // int nc_put_var_double(int ncid, int varid, const double *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var_double(Int32 ncid, Int32 varid,     
            [In(), Out()]    double[] op);
        
        // int nc_get_var_double(int ncid, int varid, double *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var_double(Int32 ncid, Int32 varid,     
            [In(), Out()]    double[] ip);


        // Additions
        // int nc_inq_grpname_len(int ncid, size_t *lenp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_grpname_len(Int32 ncid, ref Int32 lenp);

        // int nc_inq_grpname_full(int ncid, size_t *lenp, char *full_name);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_grpname_full(Int32 ncid,ref Int32 lenp, StringBuilder full_name);
        
        // int nc_inq_grpname(int ncid, char *name);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_grpname(Int32 ncid, StringBuilder name);

        //int nc_inq_grp_parent(int ncid, int *parent_ncid);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_grp_parent(Int32 ncid, ref Int32 parent_ncid);

        //int nc_inq_grps(int ncid, int *numgrps, int *ncids);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_grps(Int32 ncid, ref Int32 numgrps, 
            [In(), Out()]    Int32[] ncids);

        //int nc_def_grp(int parent_ncid, const char *name, int *new_ncid);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_grp(Int32 parent_ncid, string name, ref Int32 new_ncid);

        //int nc_inq_type(int ncid, nc_type xtype, char *name, size_t *size);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_type(Int32 ncid, Int32 xtype, StringBuilder name, ref Int32 size);

        //int nc_inq_unlimdims(int ncid, int *nunlimdimsp, int *unlimdimidsp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_unlimdims(Int32 ncid, ref Int32 numlimdimsp, [In(), Out()] Int32[] unlimdimidsp);

        //int nc_def_var_chunking(int ncid, int varid, int storage, const size_t *chunksizesp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_var_chunking(Int32 ncid, Int32 varId, Int32 storage, [In(), Out()] Int32[] chunkSizeP);

        //int nc_def_var_deflate(int ncid, int varid, int shuffle, int deflate, int deflate_level);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_var_deflate(Int32 ncid, Int32 varid, Int32 shuffle, Int32 deflate, Int32 deflate_level);

        //int nc_inq_var_chunking(int ncid, int varid, int *storagep, size_t *chunksizesp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_var_chunking(Int32 ncid, Int32 varid, ref Int32 storagep, [In(), Out()] Int32[] chunksizesp);
        
        //int nc_inq_var_deflate(int ncid, int varid, int *shufflep, int *deflatep, int *deflate_levelp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_var_deflate(int ncid, int varid, ref Int32 shufflep, ref Int32 deflatep, ref Int32 deflate_levelp);
        
        //int nc_def_var_endian(int ncid, int varid, int endian);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_var_endian(Int32 ncid, Int32 varid, Int32 endian);
        
        //int nc_inq_var_endian(int ncid, int varid, int *endianp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_var_endian(Int32 ncid, Int32 varid, ref Int32 endian);

        //int nc_def_var_fletcher32(int ncid, int varid, int fletcher32);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_var_fletcher32(Int32 ncid, Int32 varId, Int32 fletcher32);

        //int nc_inq_var_fletcher32(int ncid, int varid, int *fletcher32p);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_var_fletcher32(Int32 ncid, Int32 varid, ref Int32 fletcher32p); 

        //int nc_inq_enum(int ncid, nc_type xtype, char *name, nc_type *base_nc_typep, size_t *base_sizep, size_t *num_membersp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_enum(Int32 ncid, Int32 xtype, [In(), Out()] byte[] name, 
                ref Int32 base_nc_typep, ref Int32 base_sizep, ref Int32 num_membersp);

        //int nc_inq_typeids(int ncid, int *ntypes, int *typeids);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_typeids(Int32 ncid, ref Int32 ntypes, [In(), Out()] Int32[] typeids);

        //int nc_inq_dimids(int ncid, int *ndims, int *dimids, int include_parents);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_dimids(Int32 ncid, ref Int32 ndims, [In(), Out()] Int32[] dimids, Int32 include_parents);

        //int nc_inq_vlen(int ncid, nc_type xtype, char *name, size_t *datum_sizep, nc_type *base_nc_typep);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_vlen(Int32 ncid, Int32 xtype, [In(), Out()] byte[] name, ref Int32 datum_sizep, 
                ref Int32 nc_typep);
        
        //int nc_def_vlen(int ncid, const char *name, nc_type base_typeid, nc_type *xtypep);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_vlen(Int32 ncid, string name, Int32 base_typeid, ref Int32 xtypep);

        //int nc_put_var1(int ncid, int varid,  const size_t *indexp, const void *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1(Int32 ncid, Int32 varid, [In()] Int32[] indexp, ref vlen_t op);

        public static Int32 nc_put_var1_vlen<T>(Int32 ncid, Int32 varid, [In()] Int32[] indexp, T[] data) {
            GCHandle hdl = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                vlen_t vs = new vlen_t();
                vs.len = data.Length;
                vs.data = hdl.AddrOfPinnedObject();
                return nc_put_var1(ncid, varid, indexp, ref vs);
            } finally {
                hdl.Free();
            }
        }
        
        //int nc_put_var1(int ncid, int varid,  const size_t *indexp, const void *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var1(Int32 ncid, Int32 varid, [In()] Int32[] indexp, [In()] byte[] op);

        //int nc_get_var1(int ncid, int varid,  const size_t *indexp, void *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1(Int32 ncid, Int32 varid, [In()] Int32[] indexp, ref vlen_t ip);
        
        //int nc_get_var1(int ncid, int varid,  const size_t *indexp, void *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var1(Int32 ncid, Int32 varid, [In()] Int32[] indexp, [In(), Out()] byte[] op);

        /* Overloading for the numeric types */

        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out sbyte[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                byte[] tmp = new byte[vs.len];
                data = new sbyte[vs.len];
                Marshal.Copy(vs.data, tmp, 0, vs.len);
                for(int i=0;i<vs.len;i++)
                    data[i] = (sbyte)tmp[i];
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out byte[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                data = new byte[vs.len];
                Marshal.Copy(vs.data, data, 0, vs.len);
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out Int16[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                data = new Int16[vs.len];
                Marshal.Copy(vs.data, data, 0, vs.len);
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out UInt16[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                Int16[] tmp = new Int16[vs.len];
                data = new UInt16[vs.len];
                Marshal.Copy(vs.data, tmp, 0, vs.len);
                for(int i=0;i<vs.len;i++)
                    data[i] = (UInt16) tmp[i];
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out Int32[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                data = new Int32[vs.len];
                Marshal.Copy(vs.data, data, 0, vs.len);
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out UInt32[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                Int32[] tmp = new Int32[vs.len];
                data = new UInt32[vs.len];
                Marshal.Copy(vs.data, tmp, 0, vs.len);
                for(int i=0;i<vs.len;i++)
                    data[i] = (UInt32) tmp[i];
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out Int64[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                data = new Int64[vs.len];
                Marshal.Copy(vs.data, data, 0, vs.len);
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out UInt64[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                Int64[] tmp = new Int64[vs.len];
                data = new UInt64[vs.len];
                Marshal.Copy(vs.data, tmp, 0, vs.len);
                for(int i=0;i<vs.len;i++)
                    data[i] = (UInt64) tmp[i];
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out float[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                data = new float[vs.len];
                Marshal.Copy(vs.data, data, 0, vs.len);
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        public static Int32 nc_get_var1_vlen(Int32 ncid, Int32 varid, [In()] Int32[] indexp, out double[] data) {
            Int32 retval;
            vlen_t vs = new vlen_t();
            retval = nc_get_var1(ncid, varid, indexp, ref vs);
            try { // From this point on NetCDF allocated an array in-memory and we MUST free it
                data = new double[vs.len];
                Marshal.Copy(vs.data, data, 0, vs.len);
            } finally {
                NcCheck.Check(nc_free_vlen(ref vs));
            }
            return retval;
        }
        
        //int nc_free_vlen(nc_vlen_t *vl);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_free_vlen(ref vlen_t vl);

        //int nc_inq_user_type(int ncid, nc_type xtype, char *name, size_t *size, nc_type *base_nc_typep, size_t *nfieldsp, int *classp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_user_type(Int32 ncid, Int32 xtype, [In(), Out()] byte[] name, ref Int32 size, 
                ref Int32 base_nc_typep, ref Int32 nfieldsp, ref Int32 classp);
        //int nc_def_opaque(int ncid, size_t size, const char *name, nc_type *xtypep);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_opaque(Int32 ncid, Int32 size, string name, ref Int32 xtypep);
        
        //int nc_inq_opaque(int ncid, nc_type xtype, char *name, size_t *sizep);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_opaque(Int32 ncid, Int32 xtype, [In(), Out()] byte[] name, ref Int32 size);
        
        //int nc_def_enum(int ncid, nc_type base_typeid, const char *name, nc_type *typeidp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_def_enum(Int32 ncid, Int32 base_type_id, string name, ref Int32 typeidp);
        
        //int nc_insert_enum(int ncid, nc_type xtype, const char *name, const void *value);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_insert_enum(Int32 ncid, Int32 base_type_id, string name, ref sbyte op);
        
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_insert_enum(Int32 ncid, Int32 base_type_id, string name, ref byte op);
        
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_insert_enum(Int32 ncid, Int32 base_type_id, string name, ref Int16 op);
        
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_insert_enum(Int32 ncid, Int32 base_type_id, string name, ref UInt16 op);
        
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_insert_enum(Int32 ncid, Int32 base_type_id, string name, ref Int32 op);
        
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_insert_enum(Int32 ncid, Int32 base_type_id, string name, ref UInt32 op);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_insert_enum(Int32 ncid, Int32 base_type_id, string name, ref Int64 op);
        
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_insert_enum(Int32 ncid, Int32 base_type_id, string name, ref UInt64 op);
        
        //int nc_put_var(int ncid, int varid,  const void *op);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var(Int32 ncid, Int32 varid, [In()] sbyte[] op);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var(Int32 ncid, Int32 varid, [In()] byte[] op);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var(Int32 ncid, Int32 varid, [In()] Int16[] op);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var(Int32 ncid, Int32 varid, [In()] UInt16[] op);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var(Int32 ncid, Int32 varid, [In()] Int32[] op);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var(Int32 ncid, Int32 varid, [In()] UInt32[] op);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var(Int32 ncid, Int32 varid, [In()] Int64[] op);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_put_var(Int32 ncid, Int32 varid, [In()] UInt64[] op);

        //int nc_get_var(int ncid, int varid,  void *ip);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var(Int32 ncid, Int32 varid, [In(), Out()] sbyte[] ip);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var(Int32 ncid, Int32 varid, [In(), Out()] byte[] ip);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var(Int32 ncid, Int32 varid, [In(), Out()] Int16[] ip);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var(Int32 ncid, Int32 varid, [In(), Out()] UInt16[] ip);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var(Int32 ncid, Int32 varid, [In(), Out()] Int32[] ip);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var(Int32 ncid, Int32 varid, [In(), Out()] UInt32[] ip);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var(Int32 ncid, Int32 varid, [In(), Out()] Int64[] ip);

        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_get_var(Int32 ncid, Int32 varid, [In(), Out()] UInt64[] ip);

        /* Get enum name from enum value. Name size will be <= NC_MAX_NAME. */
        //int nc_inq_enum_ident(int ncid, nc_type xtype, long long value, char *identifier);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_enum_ident(Int32 ncid, Int32 xtype, Int64 value_op, [In(), Out()] byte[] identifier);
        
        //int nc_inq_varids(int ncid, int *nvars, int *varids);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_varids(Int32 ncid, ref Int32 nvars, [In(), Out()] Int32[] varids);

        //int nc_inq_format(int ncid, int *formatp);
        [DllImport("netcdf", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, CallingConvention=CallingConvention.Cdecl)]
        public static extern Int32 nc_inq_format(Int32 ncid, ref Int32 formatp);
        
    }
}

