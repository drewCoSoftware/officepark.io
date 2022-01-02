<template>
  <div id="site-header">
    <div>
      <h1>TimeMan</h1>
      <p>officepark.io</p>
    </div>
    <div v-if="!loginState.isLoggedIn">
      <a>Login</a>
    </div>
    <div v-else>
      <p>Welcome user! <a>Logout</a></p>
    </div>
    <div>
      <p>Login status =</p>
      <span>{{ loginState.isLoggedIn ? "yes" : "no" }}</span>
    </div>
  </div>

  <div id="nav">
    <router-link to="/">Home</router-link>
    <router-link to="/about">About</router-link>
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
  props: ["loginState"],
  inject: ["toggleLogin"],
  mounted: function () {
    // alert('i am mounted!');
    this.$cookies.set("thecookie", "whatever");
  },
};
</script>

<style lang="less" scoped>
#site-header {
  background: red;
  display: flex;
  flex-direction: row;

  > div {
    flex-grow: 1;
  }
}
</style>
