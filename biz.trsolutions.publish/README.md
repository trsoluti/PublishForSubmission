# PublishForSubmission
A simple package for Unity3D to allow students to package their projects for submission.

To most Unity users, "publish" means make available to the wider world.
However, for students, "publish" means submit your work for evaluation.

Normally, to evaluate a Unity project, four items are needed:

1. A copy of an executable build;
2. A copy of the source code, with temporary files removed;
3. A short gameplay video; and
4. Game design documentation

This package provides a one-click solution for students to ensure
all the right pieces are in the right format for submission.
Students do not have to understand the concepts of "file folders", "zip compression", "project source"
in order to prepare their submission.
The package uses the Unity defaults where possible to reduce the possibility
of files being lost.

Installing this package in your Unity project will enable a new menu item
`Publish` in your File Menu.
Clicking that item will do the following:

- Create a zipped (compressed) version of the contents of your `Build` subfolder (your build executable);
- Create a zipped (compressed) version of your project source code, with temporary files removed;
- Copy the contents of your `Recordings` folder (where the Unity Recorder stores your videos); and
- Copy the contents of your `Documentation` folder.

