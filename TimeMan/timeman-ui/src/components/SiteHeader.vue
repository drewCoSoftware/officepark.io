<template>
  <div>
    <div id="site-header">
      <div>
        <h1>TimeMan</h1>
        <p>officepark.io</p>
      </div>
      <div v-if="!loginState.State.IsLoggedIn">
        <a v-on:click="login">Login</a>
      </div>
      <div v-else>
        <p>Hello {{loginState.State.UserName}}!</p>
        <button v-on:click="logout">Logout</button>
      </div>
    </div>

    <div id="nav">
      <router-link to="/">Home</router-link>
      <router-link v-if="loginState.State.IsLoggedIn" to="/sessions"
        >Sessions</router-link>
      <router-link to="/about">About</router-link>
    </div>

    <div>
      <button v-on:click="pingTest">Ping API</button>
    </div>
  </div>
</template>

<script>
export default {
  name: "SiteHeader",
  props: ["loginState"],
  inject: ["toggleLogin", "main_logout"],
  setup() {},
  methods: {
    // logout: function()
    // {
    //     alert('i am header logout!');
    // },
    pingTest: function () {
      this.$cookies.set(
        "cookie-3",
        "a&h",
        undefined,
        undefined
        // "august-harper.com"  <-- no argument to use the current domain.
      );

      // Let's call our API!
      // OPTIONS:
      fetch("https://localhost:7001/api/pingtest", {
        credentials: "include",
      })
        .then((response) => response.json())
        .then((data) => {
          // console.log("got some data....");
          // console.dir(data);

          if (data.AuthToken == null) {
            // alert("The user is not authorized!");
            // this.other();
            // console.dir(this.logout);
            //logout();
            //console.dir(this.logout);
            this.main_logout(true);
          }
        });
    },
    logout: function () {
      this.$dtAuth.Logout();
      this.$router.push("/");
    },
    login: function () {
      this.$router.push("/login");
//      this.$dtAuth.Login("chickenman", "ABCDEF");
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
