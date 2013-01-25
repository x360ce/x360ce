#!/usr/bin/env python

# Copyright (c) 2009, Mario Vilas
# Rob Ruana 2010
# Gil Dabah
# All rights reserved.
# Licensed under GPLv3.
#

__revision__ = "$Id: setup.py 603 2010-01-31 00:11:05Z qvasimodo $"

import os
import platform
import string
import shutil
import sys

from glob import glob

from distutils import log
from distutils.command.build import build
from distutils.command.build_clib import build_clib
from distutils.command.clean import clean
from distutils.command.install_lib import install_lib
from distutils.command.sdist import sdist
from distutils.core import setup, Extension
from distutils.errors import DistutilsSetupError

from types import ListType, StringType, TupleType

from shutil import ignore_patterns

def get_sources():
    """Returns a list of C source files that should be compiled to 
    create the libdistorm3 library.
    """

    return glob('src/*.c')


class custom_build(build):
    """Customized build command"""
    def run(self):
        log.info('running custom_build')
        build.run(self)


class custom_build_clib(build_clib):
    """Customized build_clib command

    This custom_build_clib will create dynamically linked libraries rather 
    than statically linked libraries.  In addition, it places the compiled 
    libraries alongside the python packages, to facilitate the use of ctypes. 
    """

    def finalize_options (self):
        # We want build-clib to default to build-lib as defined by the 
        # "build" command.  This is so the compiled library will be put 
        # in the right place along side the python code.
        self.set_undefined_options('build',
                                   ('build_lib', 'build_clib'),
                                   ('build_temp', 'build_temp'),
                                   ('compiler', 'compiler'),
                                   ('debug', 'debug'),
                                   ('force', 'force'))

        self.libraries = self.distribution.libraries
        if self.libraries:
            self.check_library_list(self.libraries)

        if self.include_dirs is None:
            self.include_dirs = self.distribution.include_dirs or []
        if type(self.include_dirs) is StringType:
            self.include_dirs = string.split(self.include_dirs,
                                             os.pathsep)

    def get_source_files_for_lib(self, lib_name, build_info):
        sources = build_info.get('sources', [])
        if callable(sources):
            sources = sources()
        if (sources is None or 
            type(sources) not in (ListType, TupleType) or 
            len(sources) == 0):
            raise DistutilsSetupError, \
                  ("in 'libraries' option (library '%s'), "
                   "'sources' must be present and must be "
                   "a list of source filenames") % lib_name
        return sources

    def get_source_files(self):
        self.check_library_list(self.libraries)
        filenames = []
        for (lib_name, build_info) in self.libraries:
            sources = self.get_source_files_for_lib(lib_name, build_info)
            filenames.extend(sources)
        return filenames

    def run(self):
        log.info('running custom_build_clib')
        build_clib.run(self)

    def build_libraries (self, libraries):
        for (lib_name, build_info) in libraries:
            sources = self.get_source_files_for_lib(lib_name, build_info)
            sources = list(sources)

            log.info("building '%s' library", lib_name)

            # First, compile the source code to object files in the 
            # library directory.
            macros = build_info.get('macros')
            include_dirs = build_info.get('include_dirs')
            objects = self.compiler.compile(sources,
                output_dir=self.build_temp,
                macros=macros,
                include_dirs=include_dirs,
                extra_postargs=build_info.get('extra_compile_args', []),
                debug=self.debug)

            # Then link the object files and put the result in the 
            # package build directory.
            package = build_info.get('package', '')
            self.compiler.link_shared_lib(
                objects, lib_name,
                output_dir=os.path.join(self.build_clib, package),
                extra_postargs=build_info.get('extra_link_args', []),
                debug=self.debug,)


class custom_clean(clean):
    """Customized clean command

    Customized clean command removes .pyc files from the project, 
    as well as build and dist directories."""
    def run(self):
        log.info('running custom_clean')
        # Remove .pyc files
        if hasattr(os, 'walk'):
            for root, dirs, files in os.walk('.'):
                for f in files:
                    if f.endswith('.pyc'):
                        log.info("removing '%s'" % f)
                        try:
                            os.unlink(f)
                        except:
                            pass

        # Remove generated directories
        for dir in ['build', 'dist']:
            if os.path.exists(dir):
                log.info("removing '%s' (and everything under it)"%dir)
                try:
                    shutil.rmtree(dir, ignore_errors=True)
                except:
                    pass

        clean.run(self)


class custom_sdist(sdist):
    """Customized sdist command"""
    def run(self):
        log.info('running custom_sdist')
        sdist.run(self)


def main():
    # Just in case we are being called from a different directory
    cwd = os.path.dirname(__file__)
    if cwd:
        os.chdir(cwd)
    
    # Get the target platform
    system  = platform.system().lower()
    
    # Setup the extension module
    # Setup the library
    ext_modules = None
    libraries = None
    if 'windows' in system:
        libraries = [(
            'distorm3', dict(
            package='distorm3',
            sources=get_sources,
            include_dirs=['src', 'include'],
            extra_compile_args=['/Ox', '/Ob1', '/Oy', '"/D WIN32"',
                                '"/D DISTORM_DYNAMIC"', '"/D SUPPORT_64BIT_OFFSET"',
                                '"/D _MBCS"', '/GF', '/Gm', '/Zi', '/EHsc',
                                '/MT', '/Gy', '/W4', '/nologo', '/c', '/TC',
                                '/Fdbuild\\vc90.pdb'],
            extra_link_args=['/MANIFEST']))]
    elif 'darwin' in system or 'macosx' in system:
        libraries = [(
            'distorm3', dict(
            package='distorm3',
            sources=get_sources,
            include_dirs=['src', 'include'],
            extra_compile_args=['-arch', 'i386', '-arch', 'x86_64', '-O2', 
                                '-Wall', '-fPIC', '-DSUPPORT_64BIT_OFFSET', 
                                '-DDISTORM_DYNAMIC']))]
    elif 'cygwin' in system:
        libraries = [(
            'distorm3', dict(
            package='distorm3',
            sources=get_sources,
            include_dirs=['src', 'include'],
            extra_compile_args=['-fPIC', '-O2', '-Wall', 
                                '-DSUPPORT_64BIT_OFFSET', 
                                '-DDISTORM_STATIC']))]
    else:
        libraries = [(
            'distorm3', dict(
            package='distorm3',
            sources=get_sources,
            include_dirs=['src', 'include'],
            extra_compile_args=['-fPIC', '-O2', '-Wall', 
                                '-DSUPPORT_64BIT_OFFSET', 
                                '-DDISTORM_STATIC']))]
    
    options = {

    # Setup instructions
    'requires'          : ['ctypes'],
    'provides'          : ['distorm3'],
    'packages'          : ['distorm3'],
    'package_dir'       : { '' : 'python' },
    'cmdclass'          : { 'build' : custom_build,
                            'build_clib' : custom_build_clib,
                            'clean' : custom_clean, 
                            'sdist' : custom_sdist },
    'libraries'         : libraries,

    # Metadata
    'name'              : 'distorm3',
    'version'           : '3',
    'description'       : 'The goal of diStorm3 is to decode x86/AMD64' \
                          ' binary streams and return a structure that' \
                          ' describes each instruction.',
    'long_description'  : (
                        'Powerful Disassembler Library For AMD64\n'
                        'by Gil Dabah (arkon@ragestorm.net)\n'
                        '\n'
                        'Python bindings by Mario Vilas (mvilas@gmail.com)'
                        ),
    'author'            : 'Gil Dabah',
    'author_email'      : 'arkon'+chr(64)+'ragestorm'+chr(0x2e)+'net',
    'maintainer'        : 'Gil Dabah',
    'maintainer_email'  : 'arkon'+chr(64)+'ragestorm'+chr(0x2e)+'net',
    'url'               : 'http://code.google.com/p/distorm/',
    'download_url'      : 'http://code.google.com/p/distorm/',
    'platforms'         : ['cygwin', 'win', 'linux', 'macosx'],
    'classifiers'       : [
                        'License :: OSI Approved :: GPLv3 License',
                        'Development Status :: 5 - Production/Stable',
                        'Intended Audience :: Developers',
                        'Natural Language :: English',
                        'Operating System :: Microsoft :: Windows',
                        'Operating System :: MacOS :: MacOS X',
                        'Operating System :: POSIX :: Linux',
                        'Programming Language :: Python :: 2.6',
                        'Programming Language :: Python :: 2.7',
                        'Topic :: Software Development :: Disassemblers',
                        'Topic :: Software Development :: Libraries :: Python Modules',
                        ]
    }

    # Call the setup function
    setup(**options)

if __name__ == '__main__':
    main()
