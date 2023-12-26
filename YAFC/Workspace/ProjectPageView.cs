using System;
using System.Numerics;
using SDL2;
using YAFC.Model;
using YAFC.UI;
using YAFC.Workspace.ProductionTable;

namespace YAFC {
    public abstract class ProjectPageView : Scrollable {
        protected ProjectPageView() : base(true, true, false) {
            headerContent = new ImGui(BuildHeader, default, RectAllocator.LeftAlign);
            bodyContent = new ImGui(BuildContent, default, RectAllocator.LeftAlign, true);
        }

        public readonly ImGui headerContent;
        public readonly ImGui bodyContent;
        private float contentWidth, headerHeight, contentHeight;
        private SearchQuery searchQuery;
        private bool addedHandler;
        protected bool shouldSetScroll = false, hasActiveQuery = false;
        protected float targetScroll = 0f;
        protected abstract void BuildHeader(ImGui gui);
        protected abstract void BuildContent(ImGui gui);
        public virtual void SetScroll() {
            this.scroll = targetScroll;
            targetScroll = 0f;
        }

        public virtual void Rebuild(bool visualOnly = false) {
            headerContent.Rebuild();
            bodyContent.Rebuild();
        }

        protected virtual void ModelContentsChanged(bool visualOnly) {
            Rebuild(visualOnly);
        }

        public abstract void BuildPageTooltip(ImGui gui, ProjectPageContents contents);

        public abstract void SetModel(ProjectPage page);

        public virtual float CalculateWidth() {
            return headerContent.width;
        }

        public virtual void SetSearchQuery(SearchQuery query) {
            searchQuery = query;
            Rebuild();
        }

        public virtual ProjectPageView CreateSecondaryView() {
            return Activator.CreateInstance(GetType()) as ProjectPageView;
        }

        public void Build(ImGui gui, Vector2 visibleSize) {
            if (!addedHandler) {
                addedHandler = true;
                gui.AddMessageHandler<SetScrollPositionMessage>(msg => {
                    this.targetScroll = msg.Top;
                    this.shouldSetScroll = true;
                    return true;
                });
            }
            if (gui.isBuilding) {
                gui.spacing = 0f;
                var position = gui.AllocateRect(0f, 0f, 0f).Position;
                var headerSize = headerContent.CalculateState(visibleSize.X - ScrollbarSize, gui.pixelsPerUnit);
                contentWidth = headerSize.X;
                headerHeight = headerSize.Y;
                var headerRect = gui.AllocateRect(visibleSize.X, headerHeight);
                position.Y += headerHeight;
                var contentSize = bodyContent.CalculateState(visibleSize.X - ScrollbarSize, gui.pixelsPerUnit);
                if (contentSize.X > contentWidth) {
                    contentWidth = contentSize.X;
                }

                contentHeight = contentSize.Y;
                gui.DrawPanel(headerRect, headerContent);
            }
            else {
                _ = gui.AllocateRect(contentWidth, headerHeight);
            }

            // use bottom padding to enable scrolling past the last row
            base.Build(gui, visibleSize.Y - headerHeight, true);
            if (shouldSetScroll) {
                shouldSetScroll = false;
                SetScroll();
                Build(gui, visibleSize);
            }
        }

        protected override Vector2 MeasureContent(Rect rect, ImGui gui) {
            return new Vector2(contentWidth, contentHeight);
        }

        protected override void PositionContent(ImGui gui, Rect viewport) {
            headerContent.offset = new Vector2(-scrollX, 0);
            bodyContent.offset = -scroll2d;
            gui.DrawPanel(viewport, bodyContent);
        }

        public abstract void CreateModelDropdown(ImGui gui1, Type type, Project project);

        public virtual bool ControlKey(SDL.SDL_Scancode code) {
            return false;
        }

        public MemoryDrawingSurface GenerateFullPageScreenshot() {
            var headerSize = headerContent.contentSize;
            var bodySize = bodyContent.contentSize;
            Vector2 fullSize = new Vector2(CalculateWidth(), headerSize.Y + bodySize.Y);
            MemoryDrawingSurface surface = new MemoryDrawingSurface(fullSize, 22);
            surface.Clear(SchemeColor.Background.ToSdlColor());
            headerContent.Present(surface, new Rect(default, headerSize), new Rect(default, headerSize), null);
            Rect bodyRect = new Rect(0f, headerSize.Y, bodySize.X, bodySize.Y);
            var prevOffset = bodyContent.offset;
            bodyContent.offset = Vector2.Zero;
            bodyContent.Present(surface, bodyRect, bodyRect, null);
            bodyContent.offset = prevOffset;
            return surface;
        }
    }

    public abstract class ProjectPageView<T> : ProjectPageView where T : ProjectPageContents {
        protected T model;
        protected ProjectPage projectPage;

        protected override void BuildHeader(ImGui gui) {
            if (projectPage?.modelError != null && gui.BuildErrorRow(projectPage.modelError)) {
                projectPage.modelError = null;
            }
        }

        public override void SetModel(ProjectPage page) {
            if (page == null && projectPage != null) {
                Console.WriteLine("Saving scroll {0}", scroll);
                projectPage.savedScroll = scroll;
            }
            if (page != null && projectPage == null) {
                shouldSetScroll = true;
            }
            if (model != null) {
                projectPage.contentChanged -= ModelContentsChanged;
            }

            InputSystem.Instance.SetKeyboardFocus(this);
            projectPage = page;
            model = page?.content as T;
            if (model != null) {
                projectPage.contentChanged += ModelContentsChanged;
                ModelContentsChanged(false);
            }
        }

        public override void SetScroll() {
            Console.WriteLine("Writing saved scroll {0}", projectPage.savedScroll);
            this.scroll = projectPage.savedScroll;
            if (targetScroll == 0)
                this.targetScroll = projectPage.savedScroll;
            base.SetScroll();
        }

        public override void BuildPageTooltip(ImGui gui, ProjectPageContents contents) {
            BuildPageTooltip(gui, contents as T);
        }

        protected abstract void BuildPageTooltip(ImGui gui, T contents);
    }
}
