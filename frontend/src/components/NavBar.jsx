import { NavLink } from 'react-router-dom'

export default function NavBar() {
  return (
    <nav className="navbar">
      <div className="container navbar-inner">
        <span className="brand">LearnHub</span>
        <NavLink to="/" end>Courses</NavLink>
        <NavLink to="/my-courses">My Courses</NavLink>
        <NavLink to="/progress">My Progress</NavLink>
        <NavLink to="/bookmarks">My Bookmarks</NavLink>
        <NavLink to="/certificates">Certificates</NavLink>
        <NavLink to="/instructor">Instructor</NavLink>
        <NavLink to="/cart">Cart</NavLink>
        <NavLink to="/orders">My Orders</NavLink>
        <NavLink to="/instructor-revenue">Revenue</NavLink>
        <NavLink to="/dashboard">Dashboard</NavLink>
        <NavLink to="/assignments">Assignments</NavLink>
        <NavLink to="/instructor-dashboard">Instructor Stats</NavLink>
        <NavLink to="/grading">Grading</NavLink>
      </div>
    </nav>
  )
}
