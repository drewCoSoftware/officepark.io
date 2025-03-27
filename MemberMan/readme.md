# MemberMan
Code for membership management, authentication and authorization.
You may hook it to the data source of your choice.



## Key Concepts

### CheckMembership
This is an action filter that can be used to check + enforce logins for access to certain resources.

Create a sublcass of this type to handle specific cases for your application.

Any controller that uses this filter should implement the 'IHasMembershipHelper' interface, or should use a CheckMembership subclass.


### MembershipHelper
MembershipHelper is used to track and manage currently logged in users.
