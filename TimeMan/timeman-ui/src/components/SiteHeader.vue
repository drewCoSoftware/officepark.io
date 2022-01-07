<template>
  <div id="site-header">
    <div>
      <h1>TimeMan</h1>
      <p>officepark.io</p>
    </div>
    <div v-if="!loginState.isLoggedIn">
      <a>Login</a>
    </div>
  </div>

  <div id="nav">
    <router-link to="/">Home</router-link>
    <router-link v-if="loginState.isLoggedIn" to="/sessions">Sessions</router-link>
    <router-link to="/about">About</router-link>
  </div>

  <div>
    <button v-on:click="pingTest">Ping API</button>
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
  methods: {
    // Ping some API provider, hopefully with cookie goodness.
    pingTest: function () {
       this.$cookies.set("cookie-3", "a&h", undefined, undefined, 'august-harper.com')

      // Let's call our API!
      // OPTIONS:
      fetch("https://localhost:7001/api/pingtest", {
        credentials: 'include'
      })
        .then((response) => response.json())
        .then((data) => {
          console.log("got some data....");
          console.dir(data);
        });
    },
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
