<template>
  <div id="site-header">
    <div>
      <h1>TimeMan</h1>
      <p>officepark.io</p>
    </div>
    <div v-if="loginState">
        <a>Login</a>
    </div>
    <div v-else>
        <p>Welcome user! <a>Logout</a></p>
    </div>
    <div><p>Login status = </p><span>{{ loginState.isLoggedIn ? "yes" : "no" }}</span></div>
  </div>

  <div>
    <!-- This is an attempt to talk to the parent to initiate logout! -->
      <button v-on:click="toggleLogin()">Logout</button>
    </div>
</template>

<script>
export default {
  setup() {
    // This is where we could check for initial login status....
    // That means we have to send cookies around, I guess...
    // alert("I am setting up the site header!");
  },
  name: "SiteHeader",
  props: ['loginState'],
  inject: [ 'toggleLogin' ]
  // methods: {
  //   'toggleLogin': function(){
  //     // parent.toggleLogin();
  //   }
  // }
};
</script>

<style lang="less" scoped>

  #site-header{
    background:red;
    display: flex;
    flex-direction: row;

    > div {
      flex-grow:1;
    }
  }

</style>
